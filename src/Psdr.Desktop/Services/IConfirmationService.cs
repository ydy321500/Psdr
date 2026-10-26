namespace Psdr.Desktop.Services
{
    /// <summary>
    /// Presents a simple confirmation without coupling a ViewModel to WPF controls.
    /// </summary>
    public interface IConfirmationService
    {
        bool Confirm(string message);
    }
}
