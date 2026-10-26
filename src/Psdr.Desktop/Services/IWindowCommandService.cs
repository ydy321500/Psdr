namespace Psdr.Desktop.Services
{
    /// <summary>
    /// Executes shell window commands without exposing WPF controls to the ViewModel.
    /// </summary>
    public interface IWindowCommandService
    {
        void Minimize();
        void ToggleMaximize();
        void Close();
    }
}
