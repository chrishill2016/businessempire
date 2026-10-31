using MudBlazor;

namespace BlackMarketEmpire.Shared;

public static class SnackbarExtensions
{
    public const string UpdateMessage = "New version available";

    /// <summary>
    /// Shows a game message, replacing the previous one so toasts never queue up behind each other.
    /// The "new version available" prompt is left alone.
    /// </summary>
    public static void Notify(this ISnackbar snackbar, string message, bool success = true)
    {
        foreach (var shown in snackbar.ShownSnackbars.Where(s => s.Message != UpdateMessage).ToList())
        {
            snackbar.Remove(shown);
        }
        snackbar.Add(message, success ? Severity.Normal : Severity.Warning);
    }
}
