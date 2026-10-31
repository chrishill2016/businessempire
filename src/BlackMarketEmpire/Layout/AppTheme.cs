using MudBlazor;

namespace BlackMarketEmpire.Layout;

public static class AppTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#ffb300",
            PrimaryContrastText = "#121212",
            Secondary = "#26a69a",
            Background = "#121212",
            Surface = "#1e1e1e",
            AppbarBackground = "#1a1a1a",
            AppbarText = "#e0e0e0",
            Success = "#66bb6a",
            Error = "#ef5350",
        },
    };
}
