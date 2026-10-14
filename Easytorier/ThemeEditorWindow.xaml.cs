using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Easytorier;

public partial class ThemeEditorWindow : Window
{
    private bool _loading;

    public ThemeEditorWindow()
    {
        InitializeComponent();
        foreach (var theme in ThemeManager.Presets)
        {
            var button = new Button
            {
                Content = theme.Name,
                Tag = theme.Name,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.AccentBlue)),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.AccentText)),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 8, 0),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += OnPresetClick;
            PresetPanel.Children.Add(button);
        }
        LoadFields(ThemeManager.Current);
        RefreshThemeLibrary();
    }

    private void OnPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string name }) return;
        var preset = ThemeManager.Presets.FirstOrDefault(t => t.Name == name);
        if (preset is null) return;
        LoadFields(preset);
        ApplyFields();
    }

    private void LoadFields(ThemeDefinition theme)
    {
        _loading = true;
        try
        {
            ThemeNameBox.Text = theme.Name;
            BgDarkBox.Text = theme.BgDark;
            BgPanelBox.Text = theme.BgPanel;
            BgHeaderBox.Text = theme.BgHeader;
            BgCanvasBox.Text = theme.BgCanvas;
            BgInputBox.Text = theme.BgInput;
            BorderDarkBox.Text = theme.BorderDark;
            TextPrimaryBox.Text = theme.TextPrimary;
            TextMutedBox.Text = theme.TextMuted;
            AccentBlueBox.Text = theme.AccentBlue;
            AccentTextBox.Text = theme.AccentText;
            GridBaseBox.Text = theme.GridBase;
            GridLineBox.Text = theme.GridLine;
            ValidationText.Text = "Preview is live";
            ValidationText.Foreground = (Brush)Application.Current.Resources["TextMuted"];
        }
        finally { _loading = false; }
    }

    private ThemeDefinition ReadFields() => new()
    {
        Name = string.IsNullOrWhiteSpace(ThemeNameBox.Text) ? "Custom Theme" : ThemeNameBox.Text.Trim(),
        BgDark = BgDarkBox.Text.Trim(),
        BgPanel = BgPanelBox.Text.Trim(),
        BgHeader = BgHeaderBox.Text.Trim(),
        BgCanvas = BgCanvasBox.Text.Trim(),
        BgInput = BgInputBox.Text.Trim(),
        BorderDark = BorderDarkBox.Text.Trim(),
        TextPrimary = TextPrimaryBox.Text.Trim(),
        TextMuted = TextMutedBox.Text.Trim(),
        AccentBlue = AccentBlueBox.Text.Trim(),
        AccentText = AccentTextBox.Text.Trim(),
        GridBase = GridBaseBox.Text.Trim(),
        GridLine = GridLineBox.Text.Trim()
    };

    private void OnColorChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        ApplyFields();
    }

    private void ApplyFields()
    {
        var theme = ReadFields();
        if (!ThemeManager.IsValid(theme))
        {
            ValidationText.Text = "One or more colors are invalid. Use #RRGGBB or #AARRGGBB.";
            ValidationText.Foreground = Brushes.OrangeRed;
            return;
        }
        try
        {
            ThemeManager.Apply(theme);
            ValidationText.Text = "All colors look good — preview updated.";
            ValidationText.Foreground = (Brush)Application.Current.Resources["TextMuted"];
        }
        catch (Exception ex)
        {
            ValidationText.Text = ex.Message;
            ValidationText.Foreground = Brushes.OrangeRed;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var theme = ReadFields();
        if (!ThemeManager.IsValid(theme))
        {
            ApplyFields();
            return;
        }
        try
        {
            ThemeManager.Apply(theme);
            ThemeManager.SaveActive(theme);
            Directory.CreateDirectory(ThemeManager.ThemesDirectory);
            var safeName = string.Concat(theme.Name.Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-' or '_')).Trim();
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "Custom Theme";
            File.WriteAllText(Path.Combine(ThemeManager.ThemesDirectory, safeName + ".json"),
                JsonSerializer.Serialize(theme, new JsonSerializerOptions { WriteIndented = true }));
            StatusLabel.Text = "Saved '" + theme.Name + "' as your active theme and in your theme library.";
            RefreshThemeLibrary();
            SetStatusColor("TextMuted");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Couldn't save theme: " + ex.Message;
            SetStatusColor("TextMuted");
        }
    }

    private void RefreshThemeLibrary()
    {
        LibraryPanel.Children.Clear();
        if (!Directory.Exists(ThemeManager.ThemesDirectory))
        {
            LibraryLabel.Visibility = Visibility.Collapsed;
            return;
        }
        var activePath = Path.Combine(ThemeManager.ThemesDirectory, "active-theme.json");
        foreach (var path in Directory.GetFiles(ThemeManager.ThemesDirectory, "*.json")
                     .Where(file => !string.Equals(file, activePath, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var theme = ThemeManager.LoadFile(path);
                var button = new Button
                {
                    Content = theme.Name,
                    Tag = path,
                    Style = (Style)FindResource("DarkButton"),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 7, 0),
                    ToolTip = Path.GetFileName(path)
                };
                button.Click += OnLibraryThemeClick;
                LibraryPanel.Children.Add(button);
            }
            catch { }
        }
        LibraryLabel.Visibility = LibraryPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnLibraryThemeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path }) return;
        try
        {
            var theme = ThemeManager.LoadFile(path);
            LoadFields(theme);
            ApplyFields();
            StatusLabel.Text = "Loaded '" + theme.Name + "' from your theme library.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not load saved theme.\n\n" + ex.Message, "Theme library", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnImport(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Easytorier theme (*.json)|*.json|JSON files (*.json)|*.json|All files (*.*)|*.*", Title = "Import theme" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var theme = ThemeManager.LoadFile(dialog.FileName);
            LoadFields(theme);
            ApplyFields();
            StatusLabel.Text = "Imported '" + theme.Name + "'. Save theme to make it your default.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not import theme.\n\n" + ex.Message, "Import theme", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExport(object sender, RoutedEventArgs e)
    {
        var theme = ReadFields();
        if (!ThemeManager.IsValid(theme))
        {
            ApplyFields();
            return;
        }
        var dialog = new SaveFileDialog
        {
            Filter = "Easytorier theme (*.json)|*.json",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = string.Concat(theme.Name.Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-' or '_')).Trim() + ".json"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(theme, new JsonSerializerOptions { WriteIndented = true }));
            StatusLabel.Text = "Exported theme to " + dialog.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not export theme.\n\n" + ex.Message, "Export theme", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        var preset = ThemeManager.Presets[0];
        LoadFields(preset);
        ApplyFields();
        StatusLabel.Text = "Restored the Deep Slate palette. Save theme to keep it as your default.";
    }

    private void OnDone(object sender, RoutedEventArgs e) => Close();

    private void SetStatusColor(string key)
    {
        if (Application.Current.Resources[key] is Brush brush) StatusLabel.Foreground = brush;
    }
}