using MudBlazor;

namespace BlackMarketEmpire.Layout;

/// <summary>"Neon Underground" palette. Fonts and glow effects live in wwwroot/css/app.css.</summary>
public static class AppTheme
{
    public const string Background = "#07090F";
    public const string Panel = "#10141F";
    public const string Line = "#1C2436";
    public const string Acid = "#39FF88";
    public const string Magenta = "#FF3D9A";
    public const string Cyan = "#22D3EE";
    public const string Text = "#E6F0FF";
    public const string TextSecondary = "#AEBBD0";
    public const string Muted = "#8A97AD";

    public static MudTheme Theme { get; } = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = Acid,
            PrimaryContrastText = Background,
            Secondary = Magenta,
            SecondaryContrastText = Background,
            Tertiary = Cyan,
            Info = Cyan,
            Success = Acid,
            Warning = "#FACC15",
            Error = Magenta,
            Background = Background,
            BackgroundGray = Panel,
            Surface = Panel,
            DrawerBackground = Panel,
            AppbarBackground = Background,
            AppbarText = Text,
            TextPrimary = Text,
            TextSecondary = TextSecondary,
            TextDisabled = "#4A5670",
            ActionDefault = TextSecondary,
            ActionDisabled = "#4A5670",
            ActionDisabledBackground = Line,
            LinesDefault = Line,
            LinesInputs = "#243049",
            TableLines = Line,
            TableStriped = "#0C1019",
            Divider = Line,
            OverlayDark = "rgba(7,9,15,0.8)",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
        },
    };
}
