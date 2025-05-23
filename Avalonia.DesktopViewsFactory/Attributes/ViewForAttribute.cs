namespace Avalonia.DesktopViewsFactory.Attributes
{
    // Attribute for explicit binding of View and ViewModel.
    // Атрибут для явного связывания View и ViewModel.
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ViewForAttribute : Attribute
    {
        public Type ViewModelType { get; }

        public ViewForAttribute(Type viewModelType)
        {
            ViewModelType = viewModelType;
        }
    }
}
