using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.DesktopViewsFactory.Attributes;
using Avalonia.DesktopViewsFactory.Interfaces;
using ReactiveUI;

namespace Avalonia.DesktopViewsFactory.Factorys
{
    public sealed class DesktopViewsFactory : IDesktopViewsFactory, IDisposable
    {
        private readonly object _syncRoot = new();
        private bool _isDisposed;

        // Хранилище созданных окон и их ViewModel.
        private readonly ConditionalWeakTable<ReactiveObject, Window> _views = new();

        // Словарь для хранения обработчиков событий.
        private readonly Dictionary<Window, EventHandler> _windowClosedHandlers = new();

        // Cловарь для регистрации условий.
        private static readonly Dictionary<Type, Type> _viewTypeCache = new();

        // Потокобезопасная реализация синглтона.
        private static readonly Lazy<DesktopViewsFactory> _instance = new(() => new DesktopViewsFactory());

        /// <summary>
        /// Глобальный экземпляр фабрики окон.
        /// </summary>
        public static DesktopViewsFactory Instance => _instance.Value;

        /// <summary>
        /// Создать главное окно приложения.
        /// </summary>
        /// <param name="newViewModel">ViewModel для главного окна.</param>
        /// <returns>Созданное окно.</returns>
        /// <exception cref="InvalidOperationException">Если главное окно уже существует.</exception>
        /// <exception cref="ArgumentNullException">Если ViewModel равен null.</exception>
        public Window CreateMainWindow(ReactiveObject newViewModel)
        {
            lock (_syncRoot)
            {
                if (newViewModel == null)
                    throw new ArgumentNullException(nameof(newViewModel));

                // Проверка: есть ли уже главное окно.
                foreach (var entry in _views)
                {
                    // Предполагаем, что главное окно — первое созданное.
                    throw new InvalidOperationException("Main window already exists");
                }

                var view = CreateView(newViewModel);
                _views.Add(newViewModel, view);

                void Handler(object? s, EventArgs e)
                {
                    DestroyWindow(newViewModel, view);
                }

                view.Closed += Handler;
                _windowClosedHandlers[view] = Handler;

                return view;
            }
        }

        /// <summary>
        /// Получить слабую ссылку на ViewModel по имени класса представления.
        /// </summary>
        /// <param name="windowClassName">Имя класса представления.</param>
        /// <returns>Найденная ViewModel или null.</returns>
        public WeakReference<ReactiveObject>? GetViewModelInstanceWeak(string viewModelClassName)
        {
            lock (_syncRoot)
            {
                foreach (var entry in _views)
                {
                    if (entry.Key.GetType().Name.Equals(viewModelClassName, StringComparison.Ordinal))
                    {
                        return new WeakReference<ReactiveObject>(entry.Key);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Показать модальное окно с указанной ViewModel (слабая ссылка).
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="ownerViewModelWeak"></param>
        /// <param name="newViewModelWeak"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<TResult?> ShowAsyncModalWindowWeak<TResult>(
            WeakReference<ReactiveObject> ownerViewModelWeak,
            WeakReference<ReactiveObject> newViewModelWeak,
            WindowStartupLocation location = WindowStartupLocation.CenterOwner)
        {
            Window view;
            Window ownerWindow;
            ReactiveObject ownerViewModel;
            ReactiveObject newViewModel;

            lock (_syncRoot)
            {
                if (!ownerViewModelWeak.TryGetTarget(out ownerViewModel!) ||
                    !newViewModelWeak.TryGetTarget(out newViewModel!))
                {
                    throw new ObjectDisposedException("ViewModel has been garbage collected");
                }

                if (!_views.TryGetValue(ownerViewModel, out ownerWindow!))
                    throw new KeyNotFoundException("Owner window not found");

                if (_views.TryGetValue(newViewModel, out var existingWindow))
                    throw new InvalidOperationException("ViewModel already in use");

                view = CreateView(newViewModel);
                view.WindowStartupLocation = location;
                _views.Add(newViewModel, view);

                void ClosedHandler(object? s, EventArgs e) => DestroyWindow(newViewModel, view);
                view.Closed += ClosedHandler;
                _windowClosedHandlers[view] = ClosedHandler;
            }

            try
            {
                var dialogTask = view.ShowDialog<TResult?>(ownerWindow);

                if (newViewModel is ICloseable<TResult> closeable)
                {
                    var completedTask = await Task.WhenAny(
                        dialogTask,
                        closeable.Result
                    );

                    return await completedTask;
                }

                return await dialogTask;
            }
            finally
            {
                lock (_syncRoot)
                {
                    if (newViewModelWeak.TryGetTarget(out var vm))
                    {
                        if (_views.TryGetValue(vm, out var window) && window == view)
                        {
                            if (vm is ICloseable<TResult> closeable)
                            {
                                closeable.TrySetDefaultResult();
                            }

                            // Закрываем окно только если оно ещё не закрыто
                            if (window.IsVisible)
                            {
                                window.Close();
                            }

                            DestroyWindow(vm, window);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Закрыть окно, связанное с указанной ViewModel.
        /// </summary>
        /// <param name="viewModel">ViewModel закрываемого окна.</param>
        public void CloseWindowWeak(WeakReference<ReactiveObject> viewModelWeak)
        {
            lock (_syncRoot)
            {
                if (viewModelWeak.TryGetTarget(out var viewModel))
                {
                    if (_views.TryGetValue(viewModel, out var window))
                    {
                        window.Close();
                        DestroyWindow(viewModel, window);
                    }
                }
            }
        }

        #region Вспомогательные методы.

        /// <summary>
        /// Создать экземпляр окна по ViewModel.
        /// </summary>
        /// <param name="viewModel">ViewModel для создания окна.</param>
        /// <returns>Созданное окно.</returns>
        private Window CreateView(ReactiveObject viewModel)
        {
            var viewType = GetViewType(viewModel);
            var view = Activator.CreateInstance(viewType) as Window
                ?? throw new InvalidOperationException($"Failed to create {viewType.Name}");

            view.DataContext = viewModel;
            return view;
        }

        /// <summary>
        /// Получить тип представления для указанной ViewModel.
        /// </summary>
        /// <param name="viewModel">ViewModel для поиска.</param>
        /// <returns></returns>
        /// <exception cref="TypeLoadException">Если тип представления не найден.</exception>
        private Type GetViewType(ReactiveObject viewModel)
        {
            var viewModelType = viewModel.GetType();

            // 1. Проверка кэша.
            if (_viewTypeCache.TryGetValue(viewModelType, out var cachedType))
                return cachedType;

            // 2. Поиск через атрибут.
            var viewType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.GetCustomAttribute<ViewForAttribute>()?.ViewModelType == viewModelType
                    && t.IsSubclassOf(typeof(Window)));

            // 3. Fallback на соглашение имён.
            viewType ??= Assembly.GetAssembly(viewModelType)?
                .GetType(viewModelType.FullName!.Replace("ViewModel", "View"));

            if (viewType == null)
                throw new TypeLoadException(
                    $"View for {viewModelType.Name} not found. " +
                    $"Add [ViewFor(typeof({viewModelType.Name}))] to View " +
                    $"or follow naming convention.");

            // 4. Кэширование.
            _viewTypeCache[viewModelType] = viewType;
            return viewType;
        }

        /// <summary>
        /// Очистка ресурсов окна.
        /// </summary>
        /// <param name="viewModel">Связанная ViewModel.</param>
        /// <param name="window">Окно для очистки.</param>
        private void DestroyWindow(ReactiveObject viewModel, Window window)
        {

            // 1. Проверяем, существует ли связь ViewModel → Window.
            if (!_views.TryGetValue(viewModel, out var existingWindow) || existingWindow != window)
                return; // Окно уже уничтожено или ViewModel не связана.

            // 2. Удаляем обработчик закрытия окна.
            if (_windowClosedHandlers.TryGetValue(window, out var handler))
            {
                window.Closed -= handler;
                _windowClosedHandlers.Remove(window);
            }

            // 3. Удаляем ViewModel из ConditionalWeakTable.
            _views.Remove(viewModel);

            // 4. Освобождаем ресурсы ViewModel.
            if (viewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }

            // 5. Отвязываем DataContext.
            window.DataContext = null;

            Debug.WriteLine($"Метод DestroyWindow завершил уничтожение окна {window.GetType().Name}.");

#if DEBUG
            // Решение для удобства профилирования.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Debug.WriteLine($"Принудительный вызов сборщика мусора после уничтожения окна: {window.GetType().Name}.");

            bool isStillInViews = _views.TryGetValue(viewModel, out _);
            bool isStillInHandlers = _windowClosedHandlers.ContainsKey(window);

            Debug.WriteLine($"После удаления: в _views = {isStillInViews}, в обработчиках = {isStillInHandlers}");

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop1)
            {
                Debug.WriteLine($"После удаления в Application.Current.ApplicationLifetime.Windows статус наличия окна {window.GetType().Name}:  {desktop1.Windows.Contains(window)}");

                if (desktop1.Windows.Contains(window))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var stillPresent = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                            ?.Windows.Contains(window) ?? false;

                        Debug.WriteLine($"Статус наличия окна {window.GetType().Name} в списке ApplicationLifetime спустя 1 цикл диспетчера: {stillPresent}");
                    });
                }
            }
#endif
        }

        #endregion Вспомогательные методы.

        /// <summary>
        /// Освобождение ресурсов и закрытие всех окон.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_syncRoot)
            {
                foreach (var entry in _views)
                {
                    DestroyWindow(entry.Key, entry.Value);
                }

                _windowClosedHandlers.Clear();
                _isDisposed = true;
                Debug.WriteLine($"Метод Dispose завершён для {nameof(DesktopViewsFactory)}.");
            }
        }
    }
}
