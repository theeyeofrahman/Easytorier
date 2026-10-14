using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Easytorier;

public sealed class ThemeDefinition
{
    public string Name { get; set; } = "Deep Slate";
    public string BgDark { get; set; } = "#FF12151B";
    public string BgPanel { get; set; } = "#FF1D232D";
    public string BgHeader { get; set; } = "#FF273140";
    public string BgCanvas { get; set; } = "#FF11151B";
    public string BgInput { get; set; } = "#FF151A22";
    public string BorderDark { get; set; } = "#FF3C4656";
    public string TextPrimary { get; set; } = "#FFE7ECF4";
    public string TextMuted { get; set; } = "#FFA7B2C2";
    public string AccentBlue { get; set; } = "#FF4B9CFF";
    public string AccentText { get; set; } = "#FFFFFFFF";
    public string GridBase { get; set; } = "#FF171C24";
    public string GridLine { get; set; } = "#FF252D39";
}

public static class ThemeManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static event Action<ThemeDefinition>? ThemeChanged;
    public static ThemeDefinition Current { get; private set; } = new();

    public static ThemeDefinition[] Presets =>
    [
        new() { Name = "Deep Slate", AccentText="#FF102033" },
        new() { Name = "Midnight Blue", BgDark="#FF0B1020", BgPanel="#FF121B30", BgHeader="#FF192743", BgCanvas="#FF080E1A", BgInput="#FF0D1525", BorderDark="#FF2B3B5C", TextPrimary="#FFE5EDFF", TextMuted="#FF9AABD0", AccentBlue="#FF6C8CFF", AccentText="#FF10182B", GridBase="#FF0D1423", GridLine="#FF1B2940" },
        new() { Name = "Forest", BgDark="#FF101914", BgPanel="#FF1B2820", BgHeader="#FF26382C", BgCanvas="#FF0D1510", BgInput="#FF142019", BorderDark="#FF3A5141", TextPrimary="#FFE4F1E7", TextMuted="#FFA5B9AA", AccentBlue="#FF49B675", AccentText="#FF08140C", GridBase="#FF111B14", GridLine="#FF233329" },
        new() { Name = "Sunset", BgDark="#FF211719", BgPanel="#FF302124", BgHeader="#FF422A2E", BgCanvas="#FF1C1215", BgInput="#FF27191D", BorderDark="#FF624047", TextPrimary="#FFFFE9E2", TextMuted="#FFD1A9A1", AccentBlue="#FFFF805F", AccentText="#FF26100B", GridBase="#FF25171A", GridLine="#FF392326" },
        new() { Name = "Light", BgDark="#FFF1F3F7", BgPanel="#FFFFFFFF", BgHeader="#FFE2E7EF", BgCanvas="#FFF8FAFC", BgInput="#FFFFFFFF", BorderDark="#FFC7CED8", TextPrimary="#FF202733", TextMuted="#FF5D6878", AccentBlue="#FF1769D2", AccentText="#FFFFFFFF", GridBase="#FFF2F5F9", GridLine="#FFE1E6EE" },
        new() { Name = "Violet", BgDark="#FF17131F", BgPanel="#FF231D30", BgHeader="#FF30263F", BgCanvas="#FF120F19", BgInput="#FF1B1625", BorderDark="#FF4A3A5D", TextPrimary="#FFF1E9FA", TextMuted="#FFB9A9CA", AccentBlue="#FFB07CFF", AccentText="#FF1A1026", GridBase="#FF1A1424", GridLine="#FF2A2037" }
    ];

    public static string ThemesDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Easytorier", "Themes");
    private static string ActivePath => Path.Combine(ThemesDirectory, "active-theme.json");

    public static ThemeDefinition LoadActive()
    {
        try
        {
            if (File.Exists(ActivePath))
            {
                var saved = JsonSerializer.Deserialize<ThemeDefinition>(File.ReadAllText(ActivePath), JsonOptions);
                if (saved is not null && IsValid(saved)) return saved;
            }
        }
        catch { }
        return Presets[0];
    }

    public static bool IsValid(ThemeDefinition theme)
    {
        if (string.IsNullOrWhiteSpace(theme.Name)) return false;
        return TryColor(theme.BgDark) && TryColor(theme.BgPanel) && TryColor(theme.BgHeader) &&
            TryColor(theme.BgCanvas) && TryColor(theme.BgInput) && TryColor(theme.BorderDark) &&
            TryColor(theme.TextPrimary) && TryColor(theme.TextMuted) && TryColor(theme.AccentBlue) &&
            TryColor(theme.AccentText) && TryColor(theme.GridBase) && TryColor(theme.GridLine);
    }

    private static bool TryColor(string? value)
    {
        try { return value is not null && ColorConverter.ConvertFromString(value) is Color; }
        catch { return false; }
    }

    private static Color ColorFrom(string value) => (Color)ColorConverter.ConvertFromString(value);

    public static void Apply(ThemeDefinition theme)
    {
        if (!IsValid(theme)) throw new InvalidDataException("Theme contains an invalid color value.");
        Current = theme;
        var resources = Application.Current.Resources;
        resources["BgDark"] = new SolidColorBrush(ColorFrom(theme.BgDark));
        resources["BgPanel"] = new SolidColorBrush(ColorFrom(theme.BgPanel));
        resources["BgHeader"] = new SolidColorBrush(ColorFrom(theme.BgHeader));
        resources["BgCanvas"] = new SolidColorBrush(ColorFrom(theme.BgCanvas));
        resources["BgInput"] = new SolidColorBrush(ColorFrom(theme.BgInput));
        resources["BorderDark"] = new SolidColorBrush(ColorFrom(theme.BorderDark));
        resources["TextPrimary"] = new SolidColorBrush(ColorFrom(theme.TextPrimary));
        resources["TextMuted"] = new SolidColorBrush(ColorFrom(theme.TextMuted));
        resources["AccentBlue"] = new SolidColorBrush(ColorFrom(theme.AccentBlue));
        resources["AccentText"] = new SolidColorBrush(ColorFrom(theme.AccentText));
        resources["GridBase"] = new SolidColorBrush(ColorFrom(theme.GridBase));
        resources["GridLine"] = new SolidColorBrush(ColorFrom(theme.GridLine));
        ThemeChanged?.Invoke(theme);
    }

    public static void SaveActive(ThemeDefinition theme)
    {
        if (!IsValid(theme)) throw new InvalidDataException("Theme contains an invalid color value.");
        Directory.CreateDirectory(ThemesDirectory);
        File.WriteAllText(ActivePath, JsonSerializer.Serialize(theme, JsonOptions));
    }

    public static ThemeDefinition LoadFile(string path)
    {
        var theme = JsonSerializer.Deserialize<ThemeDefinition>(File.ReadAllText(path), JsonOptions);
        if (theme is null || !IsValid(theme)) throw new InvalidDataException("This file is not a valid Easytorier theme.");
        return theme;
    }

    public static DrawingBrush CreateGridBrush(ThemeDefinition theme)
    {
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(ColorFrom(theme.GridBase)),
            null, Geometry.Parse("M0,0 H32 V32 H0 Z")));
        var lines = new GeometryGroup();
        lines.Children.Add(new LineGeometry(new Point(0, 31.5), new Point(32, 31.5)));
        lines.Children.Add(new LineGeometry(new Point(31.5, 0), new Point(31.5, 32)));
        drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(ColorFrom(theme.GridLine)),
            null, lines));
        return new DrawingBrush(drawing) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, 32, 32),
            ViewportUnits = BrushMappingMode.Absolute, Opacity = 0.85 };
    }
}