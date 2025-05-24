using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.DesktopViewsFactory.Factorys;
using Avalonia.DesktopViewsFactory.Interfaces;
using ReactiveUI;

namespace DesktopAppSample.ViewModels
{
    public class ModalViewModel : ViewModelBase, IDisposable
    {
        private readonly IDesktopViewsFactory _viewsFactory = DesktopViewsFactory.Instance;
        private readonly CompositeDisposable _disposables = new();
        private bool _isDisposed;

        public event EventHandler<bool> Close;

        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        private string _title = "Не ICloseable<T> модальное окно.";

        public ReactiveCommand<Unit, Unit> ViewModelCloseCommand { get; }

        public ModalViewModel()
        {
            ViewModelCloseCommand = ReactiveCommand.Create
                (ViewModelCloseCommandMethod).DisposeWith(_disposables);
        }

        public void OnClose(bool result)
        {
            Close?.Invoke(this, result);
            Debug.WriteLine($"Вызван метод OnClose для {nameof(ModalViewModel)}.");
        }

        private void ViewModelCloseCommandMethod()
        {
            // Закрываем окно.
            _viewsFactory.CloseWindowWeak(new WeakReference<ReactiveObject>(this));
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            // Очистка события OnClose.
            Close = null!;

            // Освобождение всех подписок.
            _disposables.Dispose();

            // Подавляем вызов финализатора.
            GC.SuppressFinalize(this);

            _isDisposed = true;
            Debug.WriteLine($"Метод Dispose завершён для {nameof(ModalViewModel)}.");
        }
    }
}
