# Avalonia Desktop Views Factory

DesktopViewsFactory is a lightweight, thread-safe factory for managing windows and their lifecycle in desktop Avalonia applications built with the "Avalonia .NET MVVM App (AvaloniaUI)" template. It creates View instances from provided ViewModels, manages dialogs, and ensures resource cleanup.

## Requirements

- .NET 6.0 Desktop Runtime.
- AvaloniaUI 11.0.3.

## Features

- Creates a View corresponding to a given ViewModel, either by naming convention (`MyViewModel` → `MyView`) or explicitly using the `[ViewFor]` attribute.
- Thread-safe singleton implementation using `Lazy<T>`.
- Memory-safe with `WeakReference` and `ConditionalWeakTable`.
- Supports asynchronous dialogs through the `ICloseable<T>` interface.
- Automatically disposes of windows and associated view models when no longer needed.

## Installation

Install via NuGet:
```bash
dotnet add package Avalonia.DesktopViewsFactory
```

## Usage

See the DesktopAppSample project in the repository for a full demo.

## View Mapping

Creates a View corresponding to the provided ViewModel either by naming convention (e.g., MyViewModel → MyView) or explicitly via the [ViewFor(typeof(MyViewModel))] attribute.

## License

MIT License.

## Issues

Report bugs at [GitHub Issues](https://github.com/MaklakovSB/Avalonia.DesktopViewsFactory/issues).