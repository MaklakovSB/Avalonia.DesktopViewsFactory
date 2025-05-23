namespace Avalonia.DesktopViewsFactory.Interfaces
{
    public interface ICloseable<TResult>
    {
        public event EventHandler<TResult> Close;
        Task<TResult> Result { get; }
        void TrySetDefaultResult();
    }
}
