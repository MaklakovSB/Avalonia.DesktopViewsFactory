using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.DesktopViewsFactory.Factorys;
using Avalonia.DesktopViewsFactory.Interfaces;
using Avalonia.Markup.Xaml;
using DesktopAppSample.ViewModels;

namespace DesktopAppSample
{
    public partial class App : Application
    {
        private readonly IDesktopViewsFactory _viewsFactory = DesktopViewsFactory.Instance;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = _viewsFactory.CreateMainWindow(new MainViewModel());
                desktop.Exit += OnExit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _viewsFactory.Dispose();
        }
    }
}