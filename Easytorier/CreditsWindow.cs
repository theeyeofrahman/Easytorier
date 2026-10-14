using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Easytorier;

public sealed class CreditEntry
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}

public sealed class CreditsWindow : Window
{
    private readonly ObservableCollection<CreditEntry> _credits = new();
    private readonly DataGrid _grid = new();
    private readonly string _creditsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Easytorier", "credits.json");

    public CreditsWindow()
    {
        Title = "Easytorier Credits";
        Width = 640;
        Height = 480;
        MinWidth = 480;
        MinHeight = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = BrushResource("BgPanel", Brushes.DimGray);
        Foreground = BrushResource("TextPrimary", Brushes.White);
        BuildInterface();
        LoadCredits();
        Closing += (_, _) => { CommitGridEdits(); SaveCredits(false); };
    }

    private static Brush BrushResource(string key, Brush fallback) =>
        Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private void BuildInterface()
    {
        var root = new DockPanel { Margin = new Thickness(18) };
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        heading.Children.Add(new TextBlock
        {
            Text = "Credits",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = BrushResource("TextPrimary", Brushes.White)
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Thanks to everyone who helped build Easytorier.",
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = BrushResource("TextMuted", Brushes.LightGray)
        });
        DockPanel.SetDock(heading, Dock.Top);
        root.Children.Add(heading);

        _grid.AutoGenerateColumns = false;
        _grid.CanUserAddRows = false;
        _grid.SelectionMode = DataGridSelectionMode.Single;
        _grid.SelectionUnit = DataGridSelectionUnit.FullRow;
        _grid.HeadersVisibility = DataGridHeadersVisibility.Column;
        _grid.Margin = new Thickness(0, 0, 0, 12);
        _grid.ItemsSource = _credits;
        _grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new System.Windows.Data.Binding(nameof(CreditEntry.Name)) { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Credit", Binding = new System.Windows.Data.Binding(nameof(CreditEntry.Role)) { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged }, Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
        DockPanel.SetDock(_grid, Dock.Top);
        root.Children.Add(_grid);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var add = new Button { Content = "＋ Add credit", Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0), IsDefault = false };
        add.Click += (_, _) =>
        {
            CommitGridEdits();
            var entry = new CreditEntry { Name = "New contributor", Role = "Role" };
            _credits.Add(entry);
            _grid.SelectedItem = entry;
            _grid.ScrollIntoView(entry);
            _grid.BeginEdit();
        };
        var remove = new Button { Content = "Remove selected", Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0) };
        remove.Click += (_, _) =>
        {
            if (_grid.SelectedItem is CreditEntry selected) _credits.Remove(selected);
        };
        var save = new Button { Content = "Save credits", Padding = new Thickness(12, 7, 12, 7) };
        save.Click += (_, _) =>
        {
            CommitGridEdits();
            SaveCredits(true);
        };
        buttons.Children.Add(add);
        buttons.Children.Add(remove);
        buttons.Children.Add(save);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        Content = root;
    }

    private void CommitGridEdits()
    {
        _grid.CommitEdit(DataGridEditingUnit.Cell, true);
        _grid.CommitEdit(DataGridEditingUnit.Row, true);
    }

    private void LoadCredits()
    {
        try
        {
            if (File.Exists(_creditsPath))
            {
                var saved = JsonSerializer.Deserialize<CreditEntry[]>(File.ReadAllText(_creditsPath));
                if (saved is not null)
                {
                    foreach (var credit in saved) _credits.Add(credit);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Couldn't load saved credits. The built-in credits will be shown.\n\n{ex.Message}", "Credits", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _credits.Add(new CreditEntry { Name = "Zayden Rahman", Role = "Main programmer" });
        _credits.Add(new CreditEntry { Name = "Ghosted", Role = "Programming help" });
        _credits.Add(new CreditEntry { Name = "DC", Role = "Programmer's close friend" });
    }

    private void SaveCredits(bool showConfirmation)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_creditsPath)!);
            File.WriteAllText(_creditsPath, JsonSerializer.Serialize(_credits.ToArray(), new JsonSerializerOptions { WriteIndented = true }));
            if (showConfirmation)
                MessageBox.Show(this, "Credits saved. They'll be here next time you open Easytorier.", "Credits", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Couldn't save credits.\n\n{ex.Message}", "Credits", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
