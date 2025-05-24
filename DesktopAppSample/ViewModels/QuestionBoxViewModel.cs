using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.DesktopViewsFactory.Interfaces;
using DesktopAppSample.Enums;
using ReactiveUI;

namespace DesktopAppSample.ViewModels
{
    public class QuestionBoxViewModel : ViewModelBase, IDisposable, ICloseable<QuestionBoxResult>
    {
        private readonly TaskCompletionSource<QuestionBoxResult> _tcs = new();
        private readonly CompositeDisposable _disposables = new();
        private bool _isDisposed;

        public event EventHandler<QuestionBoxResult> Close;

        public Task<QuestionBoxResult> Result => _tcs.Task;

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }
        private string _message = string.Empty;

        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        private string _title = "Сообщение";

        public ReactiveCommand<Unit, Unit> OkCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public QuestionBoxViewModel(string message = null!, string title = null!)
        {
            if (!string.IsNullOrEmpty(title))
                Title = title;

            if (!string.IsNullOrEmpty(message))
                Message = message;

            OkCommand = ReactiveCommand.CreateFromTask(OkMethod).DisposeWith(_disposables);
            CancelCommand = ReactiveCommand.CreateFromTask(CancelMethod).DisposeWith(_disposables);
        }

        private Task OkMethod()
        {
            _tcs.TrySetResult(QuestionBoxResult.Ok);
            return Task.CompletedTask;
        }

        private Task CancelMethod()
        {
            _tcs.TrySetResult(QuestionBoxResult.Cancel);
            return Task.CompletedTask;
        }

        public void OnClose(QuestionBoxResult result)
        {
            if (_tcs.TrySetResult(result))
            {
                Close?.Invoke(this, result);
                Debug.WriteLine($"Вызван Close?.Invoke({result}) для {nameof(QuestionBoxViewModel)}.");
            }
        }

        public void TrySetDefaultResult()
        {
            if (_isDisposed) return;

            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(QuestionBoxResult.Cancel);
                Debug.WriteLine($"Вызван метод TrySetResult для {nameof(QuestionBoxViewModel)}.");
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            // Очистка события OnClose.
            Close = null!;

            TrySetDefaultResult();

            // Освобождение всех подписок.
            _disposables.Dispose();

            // Отмена TaskCompletionSource.
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetCanceled();
            }

            // Подавляем вызов финализатора.
            GC.SuppressFinalize(this);

            _isDisposed = true;
            Debug.WriteLine($"Метод Dispose завершён для {nameof(QuestionBoxViewModel)}.");
        }
    }
}
