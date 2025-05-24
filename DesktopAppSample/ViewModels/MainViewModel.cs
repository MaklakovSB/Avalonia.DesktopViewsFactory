using Avalonia.DesktopViewsFactory.Factorys;
using Avalonia.DesktopViewsFactory.Interfaces;
using DesktopAppSample.Enums;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;

namespace DesktopAppSample.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly IDesktopViewsFactory _viewsFactory = DesktopViewsFactory.Instance;
        private readonly CompositeDisposable _disposables = new();
        private bool _isDisposed;

        public ReactiveCommand<Unit, Unit> OpenQuestionBoxCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenNonICloseableModalCommand { get; }

        public MainViewModel()
        {
            OpenQuestionBoxCommand = ReactiveCommand.Create(OpenQuestionBoxCommandMethod).DisposeWith(_disposables);
            OpenNonICloseableModalCommand = ReactiveCommand.Create(OpenNonICloseableModalCommandMethod).DisposeWith(_disposables);
        }

        private async void OpenQuestionBoxCommandMethod()
        {
            var qvm = new WeakReference<ReactiveObject>(new QuestionBoxViewModel("Вы уверены?", "Вопрос"));
            var result = await _viewsFactory.ShowAsyncModalWindowWeak<QuestionBoxResult>(
                new WeakReference<ReactiveObject>(this), qvm);

            if (result == QuestionBoxResult.Ok)
            {
            }
            else
            {
            }
        }

        private async void OpenNonICloseableModalCommandMethod()
        {
            var modal_vm = new WeakReference<ReactiveObject>(new ModalViewModel());
            var result = await _viewsFactory.ShowAsyncModalWindowWeak<QuestionBoxResult>(
                new WeakReference<ReactiveObject>(this), modal_vm);

            if (result == QuestionBoxResult.Ok)
            {
            }
            else
            {
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            // Освобождение всех подписок.
            _disposables.Dispose();

            _isDisposed = true;

            Debug.WriteLine($"Метод Dispose завершён для {nameof(MainViewModel)}.");
        }
    }
}
