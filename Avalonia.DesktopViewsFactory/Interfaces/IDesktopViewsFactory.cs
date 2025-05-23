using Avalonia.Controls;
using ReactiveUI;

namespace Avalonia.DesktopViewsFactory.Interfaces
{
    public interface IDesktopViewsFactory
    {
        public Window CreateMainWindow(ReactiveObject newViewModelClassName);

        public WeakReference<ReactiveObject>? GetViewModelInstanceWeak(string viewModelClassName);

        public void CloseWindowWeak(WeakReference<ReactiveObject> viewModelWeak);

        public Task<TResult?> ShowAsyncModalWindowWeak<TResult>(
            WeakReference<ReactiveObject> ownerViewModelWeak,
            WeakReference<ReactiveObject> newViewModelWeak,
            WindowStartupLocation location = WindowStartupLocation.CenterOwner);

        public void Dispose();
    }
}
