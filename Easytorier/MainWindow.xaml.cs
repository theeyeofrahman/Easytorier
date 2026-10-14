using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Shape = System.Windows.Shapes.Shape;
using Polygon = System.Windows.Shapes.Polygon;
using Line = System.Windows.Shapes.Line;
using Rectangle = System.Windows.Shapes.Rectangle;
using Ellipse = System.Windows.Shapes.Ellipse;
using Microsoft.Win32;

namespace Easytorier;

public sealed class LevelDocument
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "Untitled Level";
    public int TotalFrames { get; set; } = 300;
    public int FramesPerSecond { get; set; } = 60;
    public List<LevelObject> Objects { get; set; } = new();
    public List<LevelParallaxLayer> ParallaxLayers { get; set; } = new();
}
public sealed class LevelParallaxLayer
{
    public string Name { get; set; } = "Background Layer";
    public string ImagePath { get; set; } = "";
    public double SpeedX { get; set; } = 0.5;
    public double SpeedY { get; set; }
    public double Opacity { get; set; } = 1;
    public double Scale { get; set; } = 1;
    public double Depth { get; set; }
    public bool Enabled { get; set; } = true;
    public string ColorHex { get; set; } = "#FF3F5268";
}
public sealed class LevelObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Object";
    public string Kind { get; set; } = "Platform";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 128;
    public double Height { get; set; } = 24;
    public double Rotation { get; set; }
    public string ColorHex { get; set; } = "";
    public string AssetPath { get; set; } = "";
    public bool TextureCollisionEnabled { get; set; }
    public int ImageType { get; set; }
    public int ImageDepth { get; set; } = 1;
    public int NativeWidth { get; set; }
    public int NativeHeight { get; set; }
    public string Tags { get; set; } = "";
    public string TriggerContentXml { get; set; } = "";
    public string LegacyXml { get; set; } = "";
    public double Opacity { get; set; } = 1;
    public double ParallaxFactor { get; set; } = 1;
    public int LayerOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public List<LevelKeyframe> Keyframes { get; set; } = new();
}
public enum EasePreset { EaseIn, Linear, EaseOut, Custom }

public sealed class PrefabEntry
{
    public string Name { get; init; } = "";
    public string PackName { get; init; } = "BASIC PREFABS";
    public string Kind { get; init; } = "Platform";
    public string Icon { get; init; } = "▰";
    public VectorTextureAsset? Texture { get; init; }
    public BitmapImage? Preview => Texture?.Preview;
}
public sealed class LevelKeyframe
{
    public int Frame { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rotation { get; set; }
    public string ColorHex { get; set; } = "#FFFFFFFF";
    public EasePreset Ease { get; set; } = EasePreset.Linear;
    public double SupportX { get; set; } = 0.5;
    public double SupportY { get; set; } = 0.5;
}

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private readonly Dictionary<string, Pose> _basePoses = new();
    private List<PrefabEntry> _prefabEntries = new();
    private List<PrefabEntry> _filteredPrefabEntries = new();
    private readonly ObservableCollection<PrefabEntry> _visiblePrefabEntries = new();
    private int _prefabVisibleCount;
    private const int PrefabBatchSize = 120;
    private sealed record Pose(double X, double Y, double Width, double Height, double Rotation, string ColorHex);
    private LevelDocument _level = new();
    private string _savedSnapshot = "";
    private string? _filePath;
    private string? _selectedId;
    private string _activeTool = "Move";
    private Point _transformStartPoint;
    private double _transformStartWidth, _transformStartHeight, _transformStartRotation;
    private string? _pendingTextureFileName;
    private int _pendingTextureWidth;
    private int _pendingTextureHeight;
    private bool _syncing;
    private int _currentFrame;
    private int TotalFrames = 300;
    private int _fps = 60;
    private bool _playing;
    private double _zoom = 1;
    private int _gridSnapSize = 16;
    private bool _isPanning;
    private bool _spaceHeld;
    private Point _panStartPoint;
    private double _panStartHorizontalOffset, _panStartVerticalOffset;
    private Brush? _gridBrush;
    private string? _draggingId;
    private Point _dragOffset;
    private bool _dragMoved;
    private string? _dragUndoSnapshot;
    private LevelKeyframe? _copiedKeyframe;
    private LevelObject? _movingKeyTrack;
    private LevelKeyframe? _movingKeyframe;
    private Ellipse? _movingKeyMarker;
    private string? _keyDragUndo;
    private bool _keyDragChanged;
    private readonly System.Windows.Threading.DispatcherTimer _timer = new(System.Windows.Threading.DispatcherPriority.Render);

    public MainWindow(string? startupLevelPath = null)
    {
        InitializeComponent();
        InitializePrefabBar();
        UpdateViewportToolButtons();
        ThemeManager.ThemeChanged += OnThemeChanged;
        ThemeManager.Apply(ThemeManager.LoadActive());
        _gridBrush = ThemeManager.CreateGridBrush(ThemeManager.Current);
        LevelViewport.Background = GridToggle.IsChecked == true ? _gridBrush : (Brush)Application.Current.Resources["BgCanvas"];
        TotalFramesBox.Text = TotalFrames.ToString(); FpsBox.Text = _fps.ToString();
        FrameSlider.Maximum = TotalFrames;
        _timer.Interval = TimeSpan.FromSeconds(1.0 / _fps);
        _timer.Tick += OnPlaybackTick;
        _savedSnapshot = Serialize();
        RefreshView();
        RenderTimeline();

        if (!string.IsNullOrWhiteSpace(startupLevelPath) && File.Exists(startupLevelPath))
        {
            try
            {
                var content = File.ReadAllText(startupLevelPath);
                var loaded = JsonSerializer.Deserialize<LevelDocument>(content, JsonOptions);
                if (loaded?.Objects is null || loaded.Version != 1)
                    throw new InvalidDataException("This project does not contain a supported Easytorier level.");
                Restore(content);
                _filePath = Path.GetFullPath(startupLevelPath);
                _savedSnapshot = Serialize();
                UpdateTitle();
                SetStatus($"Opened project level: {Path.GetFileName(startupLevelPath)}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not open the project level.\n\n{ex.Message}", "Open project", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void InitializePrefabBar()
    {
        var textures = VectorTextureAsset.LoadAll();
        var builtIns = new[]
        {
            new PrefabEntry { Name = "Platform", Kind = "Platform", Icon = "▰" },
            new PrefabEntry { Name = "Roof", Kind = "Roof", Icon = "⌃" },
            new PrefabEntry { Name = "Obstacle", Kind = "Obstacle", Icon = "■" },
            new PrefabEntry { Name = "Trigger Zone", Kind = "Trigger", Icon = "◇" },
            new PrefabEntry { Name = "Zipline", Kind = "Zipline", Icon = "╱" },
            new PrefabEntry { Name = "Player Start", Kind = "Spawn", Icon = "✦" },
            new PrefabEntry { Name = "Area", Kind = "Area", Icon = "▧" },
            new PrefabEntry { Name = "Camera", Kind = "Camera", Icon = "◉" },
            new PrefabEntry { Name = "Trapezoid", Kind = "Trapezoid", Icon = "⏢" },
            new PrefabEntry { Name = "Item", Kind = "Item", Icon = "◆" },
            new PrefabEntry { Name = "Model", Kind = "Model", Icon = "⬡" },
            new PrefabEntry { Name = "Particle", Kind = "Particle", Icon = "✣" },
            new PrefabEntry { Name = "Animation", Kind = "Animation", Icon = "▶" }
        };
        _prefabEntries = builtIns.Concat(textures.Select(texture => new PrefabEntry
        {
            Name = texture.DisplayName,
            PackName = texture.PackName,
            Kind = "Image",
            Icon = "",
            Texture = texture
        })).ToList();

        PrefabPackBox.ItemsSource = new[] { "ALL PREFABS", "BASIC PREFABS" }
            .Concat(textures.Select(texture => texture.PackName).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        PrefabPackBox.SelectedIndex = 0;
        ApplyPrefabFilter();
    }

    private void OnPrefabPackChanged(object sender, SelectionChangedEventArgs e) => ApplyPrefabFilter();
    private void OnPrefabSearchChanged(object sender, TextChangedEventArgs e) => ApplyPrefabFilter();

    private void ApplyPrefabFilter()
    {
        if (PrefabPackBox.SelectedItem is not string selectedPack || PrefabList is null) return;
        var query = PrefabSearchBox?.Text.Trim() ?? "";
        IEnumerable<PrefabEntry> entries = selectedPack switch
        {
            "BASIC PREFABS" => _prefabEntries.Where(entry => entry.Texture is null),
            "ALL PREFABS" => _prefabEntries,
            _ => _prefabEntries.Where(entry => string.Equals(entry.PackName, selectedPack, StringComparison.OrdinalIgnoreCase))
        };
        if (!string.IsNullOrEmpty(query))
            entries = entries.Where(entry => entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || entry.PackName.Contains(query, StringComparison.OrdinalIgnoreCase));
        _filteredPrefabEntries = entries.ToList();
        if (!ReferenceEquals(PrefabList.ItemsSource, _visiblePrefabEntries)) PrefabList.ItemsSource = _visiblePrefabEntries;
        PrefabList.SelectedItem = null;
        _visiblePrefabEntries.Clear();
        _prefabVisibleCount = 0;
        LoadMorePrefabEntries();
    }

    private void LoadMorePrefabEntries()
    {
        var end = Math.Min(_prefabVisibleCount + PrefabBatchSize, _filteredPrefabEntries.Count);
        while (_prefabVisibleCount < end)
            _visiblePrefabEntries.Add(_filteredPrefabEntries[_prefabVisibleCount++]);
        PrefabCountLabel.Text = _filteredPrefabEntries.Count == 0
            ? "No matching prefabs"
            : $"{_prefabVisibleCount:N0} / {_filteredPrefabEntries.Count:N0} prefabs";
        PrefabMoreButton.IsEnabled = _prefabVisibleCount < _filteredPrefabEntries.Count;
        PrefabMoreButton.Content = PrefabMoreButton.IsEnabled ? "Load more prefabs" : "All matching prefabs loaded";
    }

    private void OnLoadMorePrefabs(object sender, RoutedEventArgs e) => LoadMorePrefabEntries();

    private void OnPrefabSelected(object sender, SelectionChangedEventArgs e)
    {
        if (PrefabList.SelectedItem is PrefabEntry prefab)
            SetStatus($"Drag {prefab.Name} from the prefab bar onto the canvas to place it.");
    }

    private void OnNewLevel(object sender, RoutedEventArgs e)
    {
        if (IsDirty && MessageBox.Show(this, "Start a new level? Unsaved changes will be lost.", "New level", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        OnStopPlayback(this, new RoutedEventArgs());
        _level = new LevelDocument();
        _basePoses.Clear();
        TotalFrames = 300; _fps = 60; _currentFrame = 0;
        TotalFramesBox.Text = "300"; FpsBox.Text = "60"; FrameSlider.Maximum = TotalFrames;
        _timer.Interval = TimeSpan.FromSeconds(1.0 / _fps);
        _filePath = null;
        _selectedId = null;
        _undo.Clear(); _redo.Clear();
        _savedSnapshot = Serialize();
        RefreshView(); SetStatus("New level created.");
    }

    private void OnOpenLevel(object sender, RoutedEventArgs e)
    {
        if (IsDirty && MessageBox.Show(this, "Open another level and discard unsaved changes?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var dialog = new OpenFileDialog { Filter = "Easytorier level (*.json)|*.json|All files (*.*)|*.*", Title = "Open level" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<LevelDocument>(File.ReadAllText(dialog.FileName), JsonOptions);
            if (loaded?.Objects is null || loaded.Version != 1) throw new InvalidDataException("This file is not a supported Easytorier level.");
            OnStopPlayback(this, new RoutedEventArgs());
            _level = loaded; _basePoses.Clear(); TotalFrames = Math.Clamp(loaded.TotalFrames, 1, 10000); _fps = Math.Clamp(loaded.FramesPerSecond, 1, 240);
            TotalFramesBox.Text = TotalFrames.ToString(); FpsBox.Text = _fps.ToString(); FrameSlider.Maximum = TotalFrames;
            _timer.Interval = TimeSpan.FromSeconds(1.0 / _fps); _currentFrame = 0; _filePath = dialog.FileName; _selectedId = null; _undo.Clear(); _redo.Clear();
            ApplyFrame(0);
            _savedSnapshot = Serialize();
            RefreshView(); SetStatus($"Opened {Path.GetFileName(_filePath)}.");
        }
        catch (Exception ex) { MessageBox.Show(this, $"Could not open level.\n\n{ex.Message}", "Open level", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void OnSaveLevel(object sender, RoutedEventArgs e)
    {
        if (_filePath is null) { OnSaveAsLevel(sender, e); return; }
        SaveTo(_filePath);
    }
    private void OnSaveAsLevel(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Easytorier level (*.json)|*.json", DefaultExt = ".json", AddExtension = true, FileName = _level.Name };
        if (dialog.ShowDialog(this) == true) SaveTo(dialog.FileName);
    }
    private void SaveTo(string path)
    {
        try
        {
            _level.Name = Path.GetFileNameWithoutExtension(path);
            _level.TotalFrames = TotalFrames;
            _level.FramesPerSecond = _fps;
            var fullPath = Path.GetFullPath(path);
            var temp = fullPath + ".tmp";
            File.WriteAllText(temp, Serialize());
            File.Move(temp, fullPath, true);
            _filePath = fullPath; _savedSnapshot = Serialize(); RefreshView(); SetStatus($"Saved {Path.GetFileName(path)}.");
        }
        catch (Exception ex) { MessageBox.Show(this, $"Could not save level.\n\n{ex.Message}", "Save level", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void OnThemeChanged(ThemeDefinition theme)
    {
        if (LevelViewport is null) return;
        _gridBrush = ThemeManager.CreateGridBrush(theme);
        LevelViewport.Background = GridToggle?.IsChecked == true
            ? _gridBrush
            : (Brush)Application.Current.Resources["BgCanvas"];
        RenderCanvas();
    }

    private void OnOpenThemeEditor(object sender, RoutedEventArgs e)
    {
        new ThemeEditorWindow { Owner = this }.ShowDialog();
    }

    private void OnChooseTool(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tool }) return;
        if (tool is "Select" or "Move" or "Resize" or "Scale" or "Rotate")
        {
            SetViewportTool(tool);
            return;
        }
        if (tool == "Image")
        {
            var picker = new VectorTexturePickerWindow { Owner = this };
            if (picker.ShowDialog() != true || picker.SelectedTexture is not VectorTextureAsset texture) return;
            _pendingTextureFileName = texture.FileName;
            _pendingTextureWidth = texture.NativeWidth;
            _pendingTextureHeight = texture.NativeHeight;
            _activeTool = "Image";
            UpdateViewportToolButtons();
            SetStatus($"{texture.DisplayName} selected. Click the canvas to place it.");
            return;
        }
        _activeTool = tool;
        UpdateViewportToolButtons();
        SetStatus($"{tool} tool selected. Click the canvas to place it.");
    }
    private void SetViewportTool(string tool)
    {
        _activeTool = tool;
        UpdateViewportToolButtons();
        SetStatus(tool switch
        {
            "Select" => "Select mode: click objects to inspect them.",
            "Move" => "Move mode: drag an object to reposition it.",
            "Resize" => "Resize mode: drag an object to change its width and height.",
            "Scale" => "Scale mode: drag horizontally to scale an object proportionally.",
            _ => "Rotate mode: drag horizontally to rotate an object."
        });
    }

    private static bool IsTransformTool(string tool) => tool is "Move" or "Resize" or "Scale" or "Rotate";

    private void UpdateViewportToolButtons()
    {
        if (SelectModeButton is null) return;
        SelectModeButton.Style = (Style)FindResource(_activeTool == "Select" ? "PrimaryButton" : "DarkButton");
        MoveModeButton.Style = (Style)FindResource(_activeTool == "Move" ? "PrimaryButton" : "DarkButton");
        ResizeModeButton.Style = (Style)FindResource(_activeTool == "Resize" ? "PrimaryButton" : "DarkButton");
        ScaleModeButton.Style = (Style)FindResource(_activeTool == "Scale" ? "PrimaryButton" : "DarkButton");
        RotateModeButton.Style = (Style)FindResource(_activeTool == "Rotate" ? "PrimaryButton" : "DarkButton");
        LevelViewport.Cursor = _activeTool switch
        {
            "Move" => Cursors.SizeAll, "Resize" => Cursors.SizeNWSE, "Scale" => Cursors.SizeWE,
            "Rotate" => Cursors.Hand, "Select" => Cursors.Arrow, _ => Cursors.Cross
        };
    }

    private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement el && el.Tag is string id)
        {
            var selected = _level.Objects.FirstOrDefault(o => o.Id == id);
            if (selected is not null)
            {
                SelectObject(id);
                if (_activeTool == "Select" || !IsTransformTool(_activeTool)) { e.Handled = true; return; }
                var point = e.GetPosition(LevelViewport);
                _transformStartPoint = point;
                _transformStartWidth = selected.Width; _transformStartHeight = selected.Height;
                _transformStartRotation = selected.Rotation;
                _dragOffset = new Point(point.X - selected.X, point.Y - selected.Y);
                _draggingId = id;
                _dragMoved = false;
                _dragUndoSnapshot = null;
                LevelViewport.CaptureMouse();
                e.Handled = true;
                return;
            }
        }
        if (IsTransformTool(_activeTool)) { if (_activeTool == "Select") SelectObject(null); return; }
        if (_activeTool == "Select") { SelectObject(null); return; }
        PlaceActiveToolAt(e.GetPosition(LevelViewport));
    }

    private void PlaceActiveToolAt(Point point)
    {
        double x = point.X, y = point.Y;
        if (SnapToggle.IsChecked == true) { x = SnapToGrid(x); y = SnapToGrid(y); }
        var (w, h, label) = _activeTool switch
        {
            "Roof" or "Trapezoid" => (112d, 56d, "Roof"), "Obstacle" => (48d, 48d, "Obstacle"),
            "Trigger" => (64d, 40d, "Trigger Zone"), "Zipline" => (144d, 12d, "Zipline"),
            "Spawn" => (32d, 48d, "Player Start"), "Image" => (128d, 128d, _pendingTextureFileName is null ? "Image" : Path.GetFileNameWithoutExtension(_pendingTextureFileName)),
            _ => (128d, 24d, _activeTool == "Platform" ? "Platform" : _activeTool)
        };
        if (_activeTool == "Image" && _pendingTextureFileName is not null)
        {
            var scale = Math.Min(1d, 512d / Math.Max(_pendingTextureWidth, _pendingTextureHeight));
            w = Math.Max(1, _pendingTextureWidth * scale);
            h = Math.Max(1, _pendingTextureHeight * scale);
        }
        PushUndo();
        var item = new LevelObject { Name = label, Kind = _activeTool, X = Math.Max(0, x - w / 2), Y = Math.Max(0, y - h / 2), Width = w, Height = h, ColorHex = DefaultColor(_activeTool), AssetPath = _activeTool == "Image" ? _pendingTextureFileName ?? "" : "", NativeWidth = _pendingTextureWidth, NativeHeight = _pendingTextureHeight };
        _level.Objects.Add(item); _selectedId = item.Id;
        RefreshView(); SetStatus($"Placed {item.Name}.");
    }

    private Point _prefabDragStart;
    private void OnPrefabMouseDown(object sender, MouseButtonEventArgs e) => _prefabDragStart = e.GetPosition(PrefabList);

    private void OnPrefabMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || PrefabList.SelectedItem is not PrefabEntry prefab) return;
        var current = e.GetPosition(PrefabList);
        if (Math.Abs(current.X - _prefabDragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _prefabDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        DragDrop.DoDragDrop(PrefabList, prefab, DragDropEffects.Copy);
    }

    private void Viewport_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(PrefabEntry)) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Viewport_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(PrefabEntry)) is not PrefabEntry prefab) return;
        _activeTool = prefab.Kind;
        _pendingTextureFileName = prefab.Texture?.FileName;
        _pendingTextureWidth = prefab.Texture?.NativeWidth ?? 0;
        _pendingTextureHeight = prefab.Texture?.NativeHeight ?? 0;
        PlaceActiveToolAt(e.GetPosition(LevelViewport));
        e.Handled = true;
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingId is null || e.LeftButton != MouseButtonState.Pressed) return;
        var item = _level.Objects.FirstOrDefault(o => o.Id == _draggingId);
        if (item is null) return;
        var point = e.GetPosition(LevelViewport);
        var x = item.X; var y = item.Y; var width = item.Width; var height = item.Height; var rotation = item.Rotation;
        var dx = point.X - _transformStartPoint.X; var dy = point.Y - _transformStartPoint.Y;
        switch (_activeTool)
        {
            case "Move":
                x = Math.Max(0, point.X - _dragOffset.X);
                y = Math.Max(0, point.Y - _dragOffset.Y);
                if (SnapToggle.IsChecked == true) { x = SnapToGrid(x); y = SnapToGrid(y); }
                break;
            case "Resize":
                width = Math.Max(8, _transformStartWidth + dx);
                height = Math.Max(8, _transformStartHeight + dy);
                if (SnapToggle.IsChecked == true) { width = Math.Max(_gridSnapSize, SnapToGrid(width)); height = Math.Max(_gridSnapSize, SnapToGrid(height)); }
                break;
            case "Scale":
                var factor = Math.Clamp(1 + dx / Math.Max(100, _transformStartWidth), 0.05, 8);
                width = Math.Max(8, _transformStartWidth * factor);
                height = Math.Max(8, _transformStartHeight * factor);
                if (SnapToggle.IsChecked == true) { width = Math.Max(_gridSnapSize, SnapToGrid(width)); height = Math.Max(_gridSnapSize, SnapToGrid(height)); }
                break;
            case "Rotate":
                rotation = _transformStartRotation + dx;
                if (SnapToggle.IsChecked == true) rotation = Math.Round(rotation / 15) * 15;
                break;
        }
        if (Math.Abs(x - item.X) < 0.1 && Math.Abs(y - item.Y) < 0.1
            && Math.Abs(width - item.Width) < 0.1 && Math.Abs(height - item.Height) < 0.1
            && Math.Abs(rotation - item.Rotation) < 0.1) return;
        _dragUndoSnapshot ??= Serialize();
        item.X = x; item.Y = y; item.Width = width; item.Height = height; item.Rotation = rotation; _dragMoved = true;
        _syncing = true;
        try
        {
            XBox.Text = item.X.ToString("0.##"); YBox.Text = item.Y.ToString("0.##");
            WidthBox.Text = item.Width.ToString("0.##"); HeightBox.Text = item.Height.ToString("0.##");
            RotationBox.Text = item.Rotation.ToString("0.##");
        }
        finally { _syncing = false; }
        RenderCanvas();
        SelectionStatus.Text = $"{item.Kind} · {item.Width:0} × {item.Height:0} · {item.Rotation:0.#}°";
    }
    private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingId is null) return;
        if (_dragMoved && _dragUndoSnapshot is not null)
        {
            _undo.Push(_dragUndoSnapshot); _redo.Clear(); UpdateTitle(); RenderTimeline();
            SetStatus(_activeTool switch { "Resize" => "Object resized.", "Scale" => "Object scaled.", "Rotate" => "Object rotated.", _ => "Object moved." });
        }
        _draggingId = null; _dragUndoSnapshot = null; _dragMoved = false;
        if (LevelViewport.IsMouseCaptured) LevelViewport.ReleaseMouseCapture();
    }

    private void OnObjectSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || ObjectList.SelectedItem is not LevelObject item) return;
        SelectObject(item.Id);
    }
    private void SelectObject(string? id)
    {
        _selectedId = id;
        var item = _level.Objects.FirstOrDefault(o => o.Id == id);
        _syncing = true;
        try
        {
            ObjectList.SelectedItem = item;
            ObjectNameBox.IsEnabled = XBox.IsEnabled = YBox.IsEnabled = WidthBox.IsEnabled = HeightBox.IsEnabled = RotationBox.IsEnabled = ColorBox.IsEnabled = item is not null;
            TextureCollisionToggle.IsEnabled = ImageTypeBox.IsEnabled = ImageDepthBox.IsEnabled = item?.Kind == "Image";
            TextureCollisionToggle.IsChecked = item?.TextureCollisionEnabled == true;
            ImageTypeBox.SelectedIndex = item?.ImageType is >= 0 and <= 3 ? item.ImageType : 0;
            ImageDepthBox.SelectedIndex = item?.ImageDepth is >= 0 and <= 2 ? item.ImageDepth : 1;
            ObjectNameBox.Text = item?.Name ?? "";
            XBox.Text = item?.X.ToString("0.##") ?? "";
            YBox.Text = item?.Y.ToString("0.##") ?? "";
            WidthBox.Text = item?.Width.ToString("0.##") ?? "";
            HeightBox.Text = item?.Height.ToString("0.##") ?? "";
            RotationBox.Text = item?.Rotation.ToString("0.##") ?? "";
            ColorBox.Text = item is null ? "" : (string.IsNullOrWhiteSpace(item.ColorHex) ? DefaultColor(item.Kind) : item.ColorHex);
            UpdateKeyInspector(item);
            SelectionStatus.Text = item is null ? "No selection" : $"{item.Kind} · {item.Width:0} × {item.Height:0}";
        }
        finally { _syncing = false; }
        RenderCanvas();
        RenderTimeline();
    }

    private void OnPropertyChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncing || sender is not TextBox box) return;
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null) return;
        if (box == ObjectNameBox)
        {
            if (box.Text.Trim().Length == 0) return;
            PushUndo(); item.Name = box.Text.Trim();
        }
        else if (box == ColorBox)
        {
            try
            {
                if (ColorConverter.ConvertFromString(box.Text) is not Color parsed) return;
                PushUndo(); item.ColorHex = parsed.ToString();
                var colorKey = item.Keyframes?.FirstOrDefault(k => k.Frame == _currentFrame);
                if (colorKey is not null) colorKey.ColorHex = item.ColorHex;
            }
            catch { return; }
        }
        else
        {
            if (!double.TryParse(box.Text, out var value) || !double.IsFinite(value)) return;
            if (box == XBox && value >= 0) { PushUndo(); item.X = value; }
            else if (box == YBox && value >= 0) { PushUndo(); item.Y = value; }
            else if (box == WidthBox && value > 0) { PushUndo(); item.Width = value; }
            else if (box == HeightBox && value > 0) { PushUndo(); item.Height = value; }
            else if (box == RotationBox) { PushUndo(); item.Rotation = value; }
            else return;
            var poseKey = item.Keyframes?.FirstOrDefault(k => k.Frame == _currentFrame);
            if (poseKey is not null)
            {
                poseKey.X = item.X; poseKey.Y = item.Y; poseKey.Width = item.Width;
                poseKey.Height = item.Height; poseKey.Rotation = item.Rotation;
            }
        }
        RefreshView(true);
    }

    private void UpdateKeyInspector(LevelObject? item)
    {
        var key = item?.Keyframes?.FirstOrDefault(k => k.Frame == _currentFrame);
        SupportXBox.IsEnabled = SupportYBox.IsEnabled = key?.Ease == EasePreset.Custom;
        SupportXBox.Text = key?.SupportX.ToString("0.##") ?? "";
        SupportYBox.Text = key?.SupportY.ToString("0.##") ?? "";
        var index = key?.Ease switch { EasePreset.EaseIn => 0, EasePreset.Linear => 1, EasePreset.EaseOut => 2, EasePreset.Custom => 3, _ => 1 };
        EaseBox.SelectedIndex = index;
    }
    private void OnImageSettingsChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null || item.Kind != "Image") return;
        var type = ImageTypeBox.SelectedIndex;
        var depth = ImageDepthBox.SelectedIndex;
        if (type < 0 || depth < 0 || (item.ImageType == type && item.ImageDepth == depth)) return;
        PushUndo();
        item.ImageType = type;
        item.ImageDepth = depth;
        RefreshView(true);
        SetStatus("Updated Vector image type and depth.");
    }

    private void OnTextureCollisionChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null || item.Kind != "Image") return;
        var enabled = TextureCollisionToggle.IsChecked == true;
        if (item.TextureCollisionEnabled == enabled) return;
        PushUndo();
        item.TextureCollisionEnabled = enabled;
        RefreshView(true);
        SetStatus(enabled ? "Texture collision proxy enabled." : "Texture collision proxy removed.");
    }

    private void OnSupportChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncing || !double.TryParse(SupportXBox.Text, out var x) || !double.TryParse(SupportYBox.Text, out var y)) return;
        var key = _level.Objects.FirstOrDefault(o => o.Id == _selectedId)?.Keyframes?.FirstOrDefault(k => k.Frame == _currentFrame);
        if (key is null || key.Ease != EasePreset.Custom) return;
        PushUndo(); key.SupportX = x; key.SupportY = y; RenderTimeline();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (Palette is null || SearchBox is null) return;
        var query = SearchBox.Text.Trim();
        foreach (var button in Palette.Children.OfType<Button>())
            button.Visibility = button.Content?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ? Visibility.Visible : Visibility.Collapsed;
    }
    private string Serialize()
    {
        _level.TotalFrames = TotalFrames; _level.FramesPerSecond = _fps;
        var snapshot = JsonSerializer.Deserialize<LevelDocument>(JsonSerializer.Serialize(_level, JsonOptions), JsonOptions) ?? new();
        foreach (var item in snapshot.Objects)
        {
            if (item.Keyframes is not { Count: > 0 } || !_basePoses.TryGetValue(item.Id, out var pose)) continue;
            SetPose(item, pose);
        }
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    private static Pose CapturePose(LevelObject item) =>
        new(item.X, item.Y, item.Width, item.Height, item.Rotation, item.ColorHex);

    private static void SetPose(LevelObject item, Pose pose)
    {
        item.X = pose.X; item.Y = pose.Y; item.Width = pose.Width; item.Height = pose.Height;
        item.Rotation = pose.Rotation; item.ColorHex = pose.ColorHex;
    }

    private void EnsureBasePose(LevelObject item)
    {
        if (!_basePoses.ContainsKey(item.Id)) _basePoses[item.Id] = CapturePose(item);
    }
    private bool IsDirty => !string.Equals(Serialize(), _savedSnapshot, StringComparison.Ordinal);
    private void PushUndo() { _undo.Push(Serialize()); _redo.Clear(); }
    private void Restore(string data)
    {
        _level = JsonSerializer.Deserialize<LevelDocument>(data, JsonOptions) ?? new();
        _basePoses.Clear();
        TotalFrames = Math.Clamp(_level.TotalFrames, 1, 10000);
        _fps = Math.Clamp(_level.FramesPerSecond, 1, 240);
        _currentFrame = Math.Min(_currentFrame, TotalFrames);
        _timer.Interval = TimeSpan.FromSeconds(1.0 / _fps);
        if (!_level.Objects.Any(o => o.Id == _selectedId)) _selectedId = null;
        ApplyFrame(_currentFrame);
        RefreshView();
    }
    private void OnUndo(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0) { SetStatus("Nothing to undo."); return; }
        _redo.Push(Serialize()); Restore(_undo.Pop()); SetStatus("Undo complete.");
    }
    private void OnRedo(object sender, RoutedEventArgs e)
    {
        if (_redo.Count == 0) { SetStatus("Nothing to redo."); return; }
        _undo.Push(Serialize()); Restore(_redo.Pop()); SetStatus("Redo complete.");
    }
    private void OnDelete(object sender, RoutedEventArgs e)
    {
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null) return;
        PushUndo(); _level.Objects.Remove(item); _selectedId = null; RefreshView(); SetStatus($"Deleted {item.Name}.");
    }
    private void OnDuplicate(object sender, RoutedEventArgs e)
    {
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null) return;
        PushUndo();
        var clone = new LevelObject { Name = item.Name + " Copy", Kind = item.Kind, X = item.X + 24, Y = item.Y + 24, Width = item.Width, Height = item.Height, Rotation = item.Rotation, ColorHex = item.ColorHex, AssetPath = item.AssetPath, TextureCollisionEnabled = item.TextureCollisionEnabled, ImageType = item.ImageType, ImageDepth = item.ImageDepth, NativeWidth = item.NativeWidth, NativeHeight = item.NativeHeight, ParallaxFactor = item.ParallaxFactor, LayerOrder = item.LayerOrder, Opacity = item.Opacity, IsVisible = item.IsVisible, Tags = item.Tags, TriggerContentXml = item.TriggerContentXml, LegacyXml = item.LegacyXml };
        _level.Objects.Add(clone); _selectedId = clone.Id; RefreshView();
    }
    private void OnGenerateRoute(object sender, RoutedEventArgs e)
    {
        PushUndo();
        if (_level.Objects.Count == 0) _level.Objects.Add(new LevelObject { Name = "Starting Platform", Kind = "Platform", X = 96, Y = 700, Width = 220, Height = 28 });
        var start = _level.Objects.Where(o => o.Kind == "Platform").Select(o => o.X + o.Width).DefaultIfEmpty(300).Max();
        for (int i = 0; i < 6; i++)
            _level.Objects.Add(new LevelObject { Name = $"Jump {i + 1}", Kind = i == 3 ? "Obstacle" : "Platform", X = start + i * 170, Y = 620 - i % 3 * 90, Width = i == 3 ? 48 : 120, Height = i == 3 ? 48 : 20 });
        _selectedId = null; RefreshView(); SetStatus("Added a six-step parkour route.");
    }
    private void OnClearLevel(object sender, RoutedEventArgs e)
    {
        if (_level.Objects.Count == 0) return;
        if (MessageBox.Show(this, "Remove all objects from this level?", "Clear level", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        PushUndo(); _level.Objects.Clear(); _selectedId = null; RefreshView(); SetStatus("Level cleared.");
    }

    private void OnAddKeyframe(object sender, RoutedEventArgs e)
    {
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null) { SetStatus("Select an object before adding a keyframe."); return; }
        PushUndo();
        item.Keyframes ??= new List<LevelKeyframe>();
        if (item.Keyframes.Count == 0 && _currentFrame > 0)
            item.Keyframes.Add(new LevelKeyframe { Frame = 0, X = item.X, Y = item.Y, Width = item.Width, Height = item.Height, Rotation = item.Rotation, ColorHex = string.IsNullOrWhiteSpace(item.ColorHex) ? DefaultColor(item.Kind) : item.ColorHex, Ease = EasePreset.Linear });
        var key = item.Keyframes.FirstOrDefault(k => k.Frame == _currentFrame);
        if (key is null) { key = new LevelKeyframe { Frame = _currentFrame }; item.Keyframes.Add(key); }
        key.X = item.X; key.Y = item.Y; key.Width = item.Width; key.Height = item.Height;
        key.Rotation = item.Rotation; key.ColorHex = string.IsNullOrWhiteSpace(item.ColorHex) ? DefaultColor(item.Kind) : item.ColorHex;
        key.Ease = SelectedEase();
        item.ColorHex = key.ColorHex;
        item.Keyframes = item.Keyframes.OrderBy(k => k.Frame).ToList();
        RefreshView(); SetStatus($"Keyframe set at frame {_currentFrame}.");
    }

    private void OnDeleteKeyframe(object sender, RoutedEventArgs e)
    {
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        var key = item?.Keyframes?.FirstOrDefault(k => k.Frame == _currentFrame);
        if (item is null || key is null) { SetStatus("No keyframe at the current frame."); return; }
        PushUndo(); item.Keyframes.Remove(key); RefreshView(); SetStatus($"Removed keyframe at frame {_currentFrame}.");
    }

    private void OnEaseChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        var key = item?.Keyframes?.FirstOrDefault(k => k.Frame == _currentFrame);
        if (key is null || EaseBox.SelectedItem is not ComboBoxItem choice || !Enum.TryParse<EasePreset>(choice.Tag?.ToString(), out var ease)) return;
        PushUndo(); key.Ease = ease;
        if (ease == EasePreset.Custom) { key.SupportX = 0.35; key.SupportY = 0.65; }
        RefreshTimeline();
    }

    private EasePreset SelectedEase()
    {
        if (EaseBox.SelectedItem is ComboBoxItem choice && Enum.TryParse<EasePreset>(choice.Tag?.ToString(), out var ease)) return ease;
        return EasePreset.Linear;
    }

    private void OnFrameChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || FrameSlider is null) return;
        SetFrame((int)Math.Round(e.NewValue));
    }

    private void SetFrame(int frame, bool refreshTimeline = true)
    {
        _currentFrame = Math.Clamp(frame, 0, TotalFrames);
        _syncing = true;
        try { FrameSlider.Value = _currentFrame; FrameLabel.Text = $"Frame {_currentFrame} / {TotalFrames}"; }
        finally { _syncing = false; }
        ApplyFrame(_currentFrame);
        RenderCanvas();
        if (refreshTimeline) RenderTimeline();
    }

    private void ApplyFrame(int frame)
    {
        foreach (var item in _level.Objects)
        {
            if (item.Keyframes is not { Count: > 0 }) continue;
            EnsureBasePose(item);
            var keys = item.Keyframes.OrderBy(k => k.Frame).ToList();
            var value = Evaluate(keys, frame);
            item.X = value.X; item.Y = value.Y; item.Width = Math.Max(1, value.Width); item.Height = Math.Max(1, value.Height);
            item.Rotation = value.Rotation; item.ColorHex = value.ColorHex;
        }
        var selected = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (selected is not null) SelectObjectWithoutRender(selected);
    }

    private static LevelKeyframe Evaluate(List<LevelKeyframe> keys, int frame)
    {
        if (keys.Count == 1 || frame <= keys[0].Frame) return CopyKey(keys[0]);
        if (frame >= keys[^1].Frame) return CopyKey(keys[^1]);
        var index = 0;
        while (index < keys.Count - 1 && frame > keys[index + 1].Frame) index++;
        var a = keys[index]; var b = keys[index + 1];
        var duration = Math.Max(1, b.Frame - a.Frame);
        var t = Math.Clamp((frame - a.Frame) / (double)duration, 0, 1);
        var ease = b.Ease switch
        {
            EasePreset.EaseIn => 0d,
            EasePreset.EaseOut => 1d,
            EasePreset.Custom => double.NaN,
            _ => 0.5d
        };
        var cx = double.IsNaN(ease) ? a.X + b.SupportX : a.X + (b.X - a.X) * ease;
        var cy = double.IsNaN(ease) ? a.Y + b.SupportY : a.Y + (b.Y - a.Y) * ease;
        var x = (1 - t) * (1 - t) * a.X + 2 * (1 - t) * t * cx + t * t * b.X;
        var y = (1 - t) * (1 - t) * a.Y + 2 * (1 - t) * t * cy + t * t * b.Y;
        var deltaRotation = ((b.Rotation - a.Rotation + 540) % 360) - 180;
        return new LevelKeyframe {
            Frame = frame, X = x, Y = y,
            Width = Lerp(a.Width, b.Width, t), Height = Lerp(a.Height, b.Height, t),
            Rotation = a.Rotation + deltaRotation * t,
            ColorHex = BlendHex(a.ColorHex, b.ColorHex, t),
            Ease = b.Ease, SupportX = b.SupportX, SupportY = b.SupportY
        };
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    private static LevelKeyframe CopyKey(LevelKeyframe k) => new() {
        Frame = k.Frame, X = k.X, Y = k.Y, Width = k.Width, Height = k.Height,
        Rotation = k.Rotation, ColorHex = k.ColorHex, Ease = k.Ease, SupportX = k.SupportX, SupportY = k.SupportY
    };
    private static string BlendHex(string a, string b, double t)
    {
        try
        {
            var ca = (Color)ColorConverter.ConvertFromString(a);
            var cb = (Color)ColorConverter.ConvertFromString(b);
            byte ch(byte x, byte y) => (byte)Math.Clamp(Math.Round(x + (y - x) * t), 0, 255);
            return Color.FromArgb(ch(ca.A, cb.A), ch(ca.R, cb.R), ch(ca.G, cb.G), ch(ca.B, cb.B)).ToString();
        }
        catch { return t < 0.5 ? a : b; }
    }

    private void SelectObjectWithoutRender(LevelObject item)
    {
        _syncing = true;
        try
        {
            ObjectList.SelectedItem = item;
            ObjectNameBox.IsEnabled = XBox.IsEnabled = YBox.IsEnabled = WidthBox.IsEnabled = HeightBox.IsEnabled = RotationBox.IsEnabled = ColorBox.IsEnabled = true;
            ObjectNameBox.Text = item.Name; XBox.Text = item.X.ToString("0.##"); YBox.Text = item.Y.ToString("0.##");
            WidthBox.Text = item.Width.ToString("0.##"); HeightBox.Text = item.Height.ToString("0.##");
            RotationBox.Text = item.Rotation.ToString("0.##");
            ColorBox.Text = string.IsNullOrWhiteSpace(item.ColorHex) ? DefaultColor(item.Kind) : item.ColorHex;
            SelectionStatus.Text = $"{item.Kind} · {item.Width:0} × {item.Height:0}";
            UpdateKeyInspector(item);
        }
        finally { _syncing = false; }
    }

    private void OnTimelineSettingsChanged(object sender, RoutedEventArgs e)
    {
        var nextEnd = int.TryParse(TotalFramesBox.Text, out var end) ? Math.Clamp(end, 1, 10000) : TotalFrames;
        var nextFps = int.TryParse(FpsBox.Text, out var fps) ? Math.Clamp(fps, 1, 240) : _fps;
        if (nextEnd != TotalFrames || nextFps != _fps) PushUndo();
        TotalFrames = nextEnd; _fps = nextFps;
        TotalFramesBox.Text = TotalFrames.ToString(); FpsBox.Text = _fps.ToString();
        FrameSlider.Maximum = TotalFrames;
        foreach (var obj in _level.Objects)
            foreach (var key in obj.Keyframes ?? new List<LevelKeyframe>()) key.Frame = Math.Min(key.Frame, TotalFrames);
        foreach (var obj in _level.Objects) obj.Keyframes = (obj.Keyframes ?? new List<LevelKeyframe>()).GroupBy(k => k.Frame).Select(g => g.Last()).OrderBy(k => k.Frame).ToList();
        _currentFrame = Math.Min(_currentFrame, TotalFrames);
        _timer.Interval = TimeSpan.FromSeconds(1.0 / _fps);
        RenderTimeline(); RenderCanvas(); UpdateTitle(); SetStatus($"Timeline: {TotalFrames} frames at {_fps} FPS.");
    }

    private void CopyCurrentKeyframe()
    {
        var key = _level.Objects.FirstOrDefault(o => o.Id == _selectedId)?.Keyframes?.FirstOrDefault(k => k.Frame == _currentFrame);
        if (key is null) { SetStatus("No keyframe at the current frame to copy."); return; }
        _copiedKeyframe = CopyKey(key); SetStatus($"Copied keyframe from frame {_currentFrame}.");
    }
    private void PasteKeyframe()
    {
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null || _copiedKeyframe is null) { SetStatus("Select an object and copy a keyframe first."); return; }
        PushUndo(); item.Keyframes ??= new();
        var copy = CopyKey(_copiedKeyframe); copy.Frame = _currentFrame;
        var existing = item.Keyframes.FirstOrDefault(k => k.Frame == _currentFrame);
        if (existing is not null) item.Keyframes.Remove(existing);
        item.Keyframes.Add(copy); item.Keyframes = item.Keyframes.OrderBy(k => k.Frame).ToList();
        SetFrame(_currentFrame); RefreshView(); SetStatus($"Pasted keyframe at frame {_currentFrame}.");
    }

    private void OnPlayPause(object sender, RoutedEventArgs e)
    {
        if (_playing) { _timer.Stop(); _playing = false; PlayButton.Content = "▶ Play"; return; }
        if (!_level.Objects.Any(o => o.Keyframes?.Count > 1))
        {
            SetStatus("Add at least two keyframes to animate an object."); return;
        }
        if (_currentFrame >= TotalFrames) SetFrame(0);
        _playing = true; PlayButton.Content = "❚❚ Pause"; _timer.Start();
    }
    private void OnStopPlayback(object sender, RoutedEventArgs e)
    {
        _timer.Stop(); _playing = false; PlayButton.Content = "▶ Play"; SetFrame(0);
    }
    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        if (_currentFrame >= TotalFrames) { OnStopPlayback(this, new RoutedEventArgs()); return; }
        SetFrame(_currentFrame + 1);
    }

    private void OnTimelineChanged(object sender, RoutedEventArgs e)
    {
        // XAML can raise Checked/Unchecked while InitializeComponent is still building the window.
        if (GridToggle is null || LevelViewport is null) return;
        LevelViewport.Background = GridToggle.IsChecked == true
            ? (_gridBrush ?? LevelViewport.Background)
            : (Brush)Application.Current.Resources["BgCanvas"];
        RenderCanvas();
        RenderTimeline();
    }
    private void RefreshTimeline()
    {
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        _syncing = true;
        try { UpdateKeyInspector(item); }
        finally { _syncing = false; }
        RenderTimeline();
    }
    private void RenderTimeline()
    {
        if (TimelineCanvas is null) return;
        TimelineCanvas.Children.Clear();
        const double left = 8, width = 1184, y = 21;
        for (var f = 0; f <= TotalFrames; f += 30)
        {
            var x = left + width * f / TotalFrames;
            var line = new Line { X1 = x, X2 = x, Y1 = 4, Y2 = 46, Stroke = new SolidColorBrush(Color.FromRgb(55, 59, 66)), StrokeThickness = 1 };
            TimelineCanvas.Children.Add(line);
            var label = new TextBlock { Text = f.ToString(), Foreground = new SolidColorBrush(Color.FromRgb(135, 140, 150)), FontSize = 9 };
            Canvas.SetLeft(label, x + 2); Canvas.SetTop(label, 2); TimelineCanvas.Children.Add(label);
        }
        var selected = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (selected?.Keyframes is not null)
        {
            foreach (var key in selected.Keyframes)
            {
                var dot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.DeepSkyBlue, Stroke = Brushes.White, StrokeThickness = 1, Tag = key.Frame, ToolTip = $"Frame {key.Frame} · {key.Ease}" };
                Canvas.SetLeft(dot, left + width * key.Frame / TotalFrames - 5); Canvas.SetTop(dot, y); TimelineCanvas.Children.Add(dot);
            }
        }
        var cursorX = left + width * _currentFrame / TotalFrames;
        TimelineCanvas.Children.Add(new Line { X1 = cursorX, X2 = cursorX, Y1 = 0, Y2 = 48, Stroke = Brushes.Orange, StrokeThickness = 2 });
        _syncing = true;
        try { FrameSlider.Value = _currentFrame; FrameLabel.Text = $"Frame {_currentFrame} / {TotalFrames}"; }
        finally { _syncing = false; }
    }
    private void Timeline_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { Tag: int keyFrame } marker)
        {
            var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
            var key = item?.Keyframes?.FirstOrDefault(k => k.Frame == keyFrame);
            if (item is not null && key is not null)
            {
                _movingKeyTrack = item; _movingKeyframe = key; _movingKeyMarker = marker as Ellipse;
                _keyDragUndo = null; _keyDragChanged = false;
                SetFrame(keyFrame, false); TimelineCanvas.CaptureMouse(); e.Handled = true; return;
            }
        }
        var x = e.GetPosition(TimelineCanvas).X;
        SetFrame((int)Math.Round(Math.Clamp((x - 8) / 1184, 0, 1) * TotalFrames));
        TimelineCanvas.CaptureMouse();
    }
    private void Timeline_MouseMove(object sender, MouseEventArgs e)
    {
        if (!TimelineCanvas.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;
        var x = e.GetPosition(TimelineCanvas).X;
        var frame = (int)Math.Round(Math.Clamp((x - 8) / 1184, 0, 1) * TotalFrames);
        if (_movingKeyframe is not null && _movingKeyTrack is not null)
        {
            if (frame == _movingKeyframe.Frame) return;
            _keyDragUndo ??= Serialize();
            _keyDragChanged = true;
            _movingKeyframe.Frame = frame;
            _currentFrame = frame;
            if (_movingKeyMarker is not null) Canvas.SetLeft(_movingKeyMarker, 8 + 1184 * frame / TotalFrames - 5);
            _syncing = true;
            try { FrameSlider.Value = frame; FrameLabel.Text = $"Frame {frame} / {TotalFrames}"; }
            finally { _syncing = false; }
            RenderCanvas();
            return;
        }
        SetFrame(frame);
    }
    private void Timeline_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_movingKeyframe is not null && _movingKeyTrack is not null)
        {
            if (_keyDragChanged && _keyDragUndo is not null)
            {
                _movingKeyTrack.Keyframes.RemoveAll(k => !ReferenceEquals(k, _movingKeyframe) && k.Frame == _movingKeyframe.Frame);
                _movingKeyTrack.Keyframes = _movingKeyTrack.Keyframes.OrderBy(k => k.Frame).ToList();
                _undo.Push(_keyDragUndo); _redo.Clear();
                SetStatus($"Moved keyframe to frame {_movingKeyframe.Frame}.");
                ApplyFrame(_currentFrame);
                RefreshView();
            }
            _movingKeyTrack = null; _movingKeyframe = null; _movingKeyMarker = null; _keyDragUndo = null; _keyDragChanged = false;
        }
        if (TimelineCanvas.IsMouseCaptured) TimelineCanvas.ReleaseMouseCapture();
    }
    private void OnZoomIn(object sender, RoutedEventArgs e) => SetZoom(_zoom + 0.1);
    private void OnZoomOut(object sender, RoutedEventArgs e) => SetZoom(_zoom - 0.1);
    private void OnZoomReset(object sender, RoutedEventArgs e) => SetZoom(1);

    private void OnZoomFit(object sender, RoutedEventArgs e)
    {
        if (ViewportScrollViewer is null || LevelViewport is null) return;
        var width = ViewportScrollViewer.ViewportWidth;
        var height = ViewportScrollViewer.ViewportHeight;
        if (width <= 0 || height <= 0) return;
        SetZoom(Math.Min((width - 24) / LevelViewport.Width, (height - 24) / LevelViewport.Height));
        ViewportScrollViewer.ScrollToHorizontalOffset(0);
        ViewportScrollViewer.ScrollToVerticalOffset(0);
    }

    private void OnGridSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridSizeBox?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var size) && size > 0)
        {
            _gridSnapSize = size;
            if (StatusText is not null) SetStatus($"Grid snap spacing: {_gridSnapSize} units.");
        }
    }

    private double SnapToGrid(double value) => Math.Round(value / _gridSnapSize) * _gridSnapSize;

    private void SetZoom(double zoom)
    {
        _zoom = Math.Clamp(zoom, 0.35, 2.5);
        LevelViewport.LayoutTransform = new ScaleTransform(_zoom, _zoom);
        if (ZoomLabelButton is not null) ZoomLabelButton.Content = $"{_zoom:P0}";
        SetStatus($"Viewport zoom: {_zoom:P0}.");
    }

    private void Viewport_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle && !(_spaceHeld && e.ChangedButton == MouseButton.Left)) return;
        _isPanning = true;
        _panStartPoint = e.GetPosition(ViewportScrollViewer);
        _panStartHorizontalOffset = ViewportScrollViewer.HorizontalOffset;
        _panStartVerticalOffset = ViewportScrollViewer.VerticalOffset;
        ViewportScrollViewer.Cursor = Cursors.Hand;
        ViewportScrollViewer.CaptureMouse();
        e.Handled = true;
    }

    private void Viewport_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;
        var point = e.GetPosition(ViewportScrollViewer);
        ViewportScrollViewer.ScrollToHorizontalOffset(_panStartHorizontalOffset - (point.X - _panStartPoint.X));
        ViewportScrollViewer.ScrollToVerticalOffset(_panStartVerticalOffset - (point.Y - _panStartPoint.Y));
        e.Handled = true;
    }

    private void Viewport_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning || (e.ChangedButton != MouseButton.Middle && e.ChangedButton != MouseButton.Left)) return;
        _isPanning = false;
        ViewportScrollViewer.ReleaseMouseCapture();
        ViewportScrollViewer.Cursor = LevelViewport.Cursor;
        e.Handled = true;
    }

    private static string DefaultColor(string kind) => kind switch {
        "Obstacle" => "#FFCD5C5C", "Trigger" => "#FFFFD700", "Roof" => "#FF696969",
        "Zipline" => "#FF00BFFF", "Spawn" => "#FF32CD32", _ => "#FF708090"
    };
    private UIElement CreateShape(LevelObject o, bool ghost = false)
    {
        if (o.Kind == "Image" && VectorTextureAsset.LoadBitmap(o.AssetPath) is { } bitmap)
        {
            var image = new System.Windows.Controls.Image { Source = bitmap, Stretch = Stretch.Fill, IsHitTestVisible = false, Opacity = ghost ? .22 : Math.Clamp(o.Opacity, 0, 1) };
            var border = new Border
            {
                Width = o.Width, Height = o.Height, Background = Brushes.Transparent, Child = image,
                BorderBrush = ghost ? Brushes.DeepSkyBlue : o.Id == _selectedId ? Brushes.White : Brushes.Transparent,
                BorderThickness = ghost ? new Thickness(1) : o.Id == _selectedId ? new Thickness(2) : new Thickness(0),
                RenderTransform = new RotateTransform(o.Rotation, o.Width / 2, o.Height / 2),
                Tag = ghost ? null : o.Id, ToolTip = o.Name
            };
            Canvas.SetLeft(border, o.X); Canvas.SetTop(border, o.Y);
            return border;
        }

        Brush fallback = o.Kind switch { "Obstacle" => Brushes.IndianRed, "Trigger" => Brushes.Gold, "Roof" => Brushes.DimGray, "Zipline" => Brushes.DeepSkyBlue, "Spawn" => Brushes.LimeGreen, _ => Brushes.SlateGray };
        Brush color = fallback;
        try { if (!string.IsNullOrWhiteSpace(o.ColorHex) && ColorConverter.ConvertFromString(o.ColorHex) is Color parsed) color = new SolidColorBrush(parsed); } catch { }
        Shape shape;
        if (o.Kind is "Roof" or "Trapezoid") shape = new Polygon { Points = new PointCollection { new(0, o.Height), new(o.Width / 2, 0), new(o.Width, o.Height) }, Fill = color };
        else if (o.Kind == "Zipline") shape = new Line { X1 = 0, Y1 = o.Height, X2 = o.Width, Y2 = 0, Stroke = color, StrokeThickness = 4 };
        else shape = new Rectangle { Fill = color, RadiusX = o.Kind == "Trigger" ? 5 : 1, RadiusY = o.Kind == "Trigger" ? 5 : 1, Opacity = o.Kind == "Trigger" ? .55 : Math.Clamp(o.Opacity, 0, 1) };
        if (ghost) { shape.Opacity = .22; shape.Stroke = Brushes.DeepSkyBlue; shape.StrokeThickness = 1; }
        else { shape.Stroke = o.Id == _selectedId ? Brushes.White : Brushes.Black; shape.StrokeThickness = o.Id == _selectedId ? 3 : 1; }
        shape.Width = o.Width; shape.Height = o.Height;
        shape.RenderTransform = new RotateTransform(o.Rotation, o.Width / 2, o.Height / 2);
        if (!ghost) shape.Tag = o.Id;
        Canvas.SetLeft(shape, o.X); Canvas.SetTop(shape, o.Y);
        return shape;
    }

    private void RenderCanvas()
    {
        if (LevelViewport is null) return;
        LevelViewport.Children.Clear();
        var selected = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (OnionToggle?.IsChecked == true && selected?.Keyframes is { Count: > 1 } keys)
        {
            var ordered = keys.OrderBy(k => k.Frame).ToList();
            var prev = ordered.LastOrDefault(k => k.Frame < _currentFrame);
            var next = ordered.FirstOrDefault(k => k.Frame > _currentFrame);
            if (prev is not null) LevelViewport.Children.Add(CreateShape(AtKey(selected, prev), true));
            if (next is not null) LevelViewport.Children.Add(CreateShape(AtKey(selected, next), true));
        }
        foreach (var item in _level.Objects.Where(o => o.IsVisible).OrderBy(o => o.LayerOrder).ThenBy(o => o.Y).ThenBy(o => o.X))
        {
            LevelViewport.Children.Add(CreateShape(item));
            if (item.Kind == "Image" && item.TextureCollisionEnabled)
            {
                var bounds = VectorierXmlSerializer.RotatedBounds(item.X, item.Y, item.Width, item.Height, item.Rotation);
                var collision = new Rectangle
                {
                    Width = bounds.Width, Height = bounds.Height, Fill = Brushes.Transparent,
                    Stroke = Brushes.OrangeRed, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 5, 3 },
                    IsHitTestVisible = false, Tag = item.Id + ":collision"
                };
                Canvas.SetLeft(collision, bounds.X); Canvas.SetTop(collision, bounds.Y);
                LevelViewport.Children.Add(collision);
            }
        }
    }
    private static LevelObject AtKey(LevelObject source, LevelKeyframe key) => new() {
        Id = source.Id, Name = source.Name, Kind = source.Kind, X = key.X, Y = key.Y, Width = key.Width,
        Height = key.Height, Rotation = key.Rotation, ColorHex = key.ColorHex, AssetPath = source.AssetPath,
        NativeWidth = source.NativeWidth, NativeHeight = source.NativeHeight, Opacity = source.Opacity
    };
    private void RefreshView(bool keepInspector = false)
    {
        _syncing = true;
        try
        {
            ObjectList.ItemsSource = null; ObjectList.ItemsSource = _level.Objects; ObjectList.DisplayMemberPath = nameof(LevelObject.Name);
            ObjectList.SelectedItem = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
            ObjectCountText.Text = $"{_level.Objects.Count} object{(_level.Objects.Count == 1 ? "" : "s")}";
            LevelNameLabel.Text = _level.Name;
            if (!keepInspector)
            {
                var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
                ObjectNameBox.IsEnabled = XBox.IsEnabled = YBox.IsEnabled = WidthBox.IsEnabled = HeightBox.IsEnabled = RotationBox.IsEnabled = ColorBox.IsEnabled = item is not null;
                TextureCollisionToggle.IsEnabled = ImageTypeBox.IsEnabled = ImageDepthBox.IsEnabled = item?.Kind == "Image";
                TextureCollisionToggle.IsChecked = item?.TextureCollisionEnabled == true;
                ImageTypeBox.SelectedIndex = item?.ImageType is >= 0 and <= 3 ? item.ImageType : 0;
                ImageDepthBox.SelectedIndex = item?.ImageDepth is >= 0 and <= 2 ? item.ImageDepth : 1;
                ObjectNameBox.Text = item?.Name ?? ""; XBox.Text = item?.X.ToString("0.##") ?? ""; YBox.Text = item?.Y.ToString("0.##") ?? "";
                WidthBox.Text = item?.Width.ToString("0.##") ?? ""; HeightBox.Text = item?.Height.ToString("0.##") ?? "";
                RotationBox.Text = item?.Rotation.ToString("0.##") ?? "";
                ColorBox.Text = item is null ? "" : (string.IsNullOrWhiteSpace(item.ColorHex) ? DefaultColor(item.Kind) : item.ColorHex);
                UpdateKeyInspector(item);
                SelectionStatus.Text = item is null ? "No selection" : $"{item.Kind} · {item.Width:0} × {item.Height:0}";
            }
        }
        finally { _syncing = false; }
        FrameSlider.Maximum = TotalFrames;
        TotalFramesBox.Text = TotalFrames.ToString(); FpsBox.Text = _fps.ToString();
        RenderCanvas();
        RenderTimeline();
        UpdateTitle();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None && Keyboard.FocusedElement is not TextBox && Keyboard.FocusedElement is not ComboBox)
        {
            _spaceHeld = true;
            if (!_isPanning && ViewportScrollViewer is not null) ViewportScrollViewer.Cursor = Cursors.Hand;
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.None && Keyboard.FocusedElement is not TextBox && Keyboard.FocusedElement is not ComboBox && e.Key is Key.D1 or Key.D2 or Key.D3 or Key.D4 or Key.D5)
        {
            var tool = e.Key switch { Key.D1 => "Select", Key.D2 => "Move", Key.D3 => "Resize", Key.D4 => "Scale", _ => "Rotate" };
            SetViewportTool(tool);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) { OnNewLevel(this, new()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O) { OnOpenLevel(this, new()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S) { OnSaveLevel(this, new()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z) { OnUndo(this, new()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y) { OnRedo(this, new()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D) { OnDuplicate(this, new()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C && Keyboard.FocusedElement is not TextBox) { CopyCurrentKeyframe(); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V && Keyboard.FocusedElement is not TextBox) { PasteKeyframe(); e.Handled = true; }
        else if (e.Key == Key.Delete && Keyboard.FocusedElement is not TextBox) { OnDelete(this, new()); e.Handled = true; }
        else if (e.Key == Key.Escape) { _activeTool = "Select"; SelectObject(null); SetStatus("Selection tool active."); }
    }
    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space) return;
        _spaceHeld = false;
        if (!_isPanning && ViewportScrollViewer is not null) ViewportScrollViewer.Cursor = LevelViewport.Cursor;
    }

    private void OnPreview(object sender, RoutedEventArgs e)
    {
        var canvas = new Canvas { Width = LevelViewport.Width, Height = LevelViewport.Height, Background = (Brush)Application.Current.Resources["BgCanvas"] };
        foreach (var item in _level.Objects.Where(o => o.IsVisible).OrderBy(o => o.LayerOrder).ThenBy(o => o.Y).ThenBy(o => o.X)) canvas.Children.Add(CreateShape(item));
        new Window { Title = $"{_level.Name} — Preview", Width = 900, Height = 600, Owner = this, Content = new ScrollViewer { Content = canvas } }.ShowDialog();
    }
    private void OnExportXml(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Vectorier level XML (*.xml)|*.xml",
            DefaultExt = ".xml",
            AddExtension = true,
            FileName = _level.Name
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var xml = VectorierXmlSerializer.Export(_level);
            var fullPath = Path.GetFullPath(dialog.FileName);
            var tempPath = fullPath + ".tmp";
            File.WriteAllText(tempPath, xml, new System.Text.UTF8Encoding(false));
            File.Move(tempPath, fullPath, true);
            SetStatus($"Compiled {_level.Objects.Count(o => o.IsVisible)} visible objects to {Path.GetFileName(fullPath)}.");
            MessageBox.Show(this, "Vectorier XML was compiled successfully.", "Compile XML", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not compile Vectorier XML.\n\n{ex.Message}", "Compile XML", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnImportXml(object sender, RoutedEventArgs e)
    {
        if (IsDirty && MessageBox.Show(this, "Import XML and replace the current level? Unsaved changes will be lost.", "Import Vectorier XML", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var dialog = new OpenFileDialog { Filter = "Vectorier level XML (*.xml)|*.xml|All files (*.*)|*.*", Title = "Import Vectorier XML" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var loaded = VectorierXmlSerializer.Import(dialog.FileName);
            if (loaded.Objects.Count == 0) throw new InvalidDataException("No objects were found under Root/Track/Object/Content.");
            _undo.Push(Serialize()); _redo.Clear();
            OnStopPlayback(this, new RoutedEventArgs());
            _level = loaded;
            _basePoses.Clear(); _currentFrame = 0; _filePath = null; _selectedId = null;
            _savedSnapshot = Serialize();
            RefreshView();
            SetStatus($"Imported {loaded.Objects.Count} objects from Vectorier XML.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not import Vectorier XML.\n\n{ex.Message}", "Import Vectorier XML", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnEditTrigger(object sender, RoutedEventArgs e)
    {
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null || item.Kind != "Trigger") return;
        var editor = new TriggerEditorWindow(item.TriggerContentXml, item.Name) { Owner = this };
        if (editor.ShowDialog() != true) return;
        PushUndo();
        item.TriggerContentXml = editor.TriggerXml;
        RefreshView(true);
        SetStatus($"Updated trigger logic for {item.Name}.");
    }

    private void OnEditObjectProperties(object sender, RoutedEventArgs e)
    {
        var item = _level.Objects.FirstOrDefault(o => o.Id == _selectedId);
        if (item is null) return;
        var dialog = new Window { Title = $"Advanced properties — {item.Name}", Width = 410, Height = 390, MinWidth = 360, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, Background = (Brush)Application.Current.Resources["BgPanel"], Foreground = (Brush)Application.Current.Resources["TextPrimary"] };
        var panel = new StackPanel { Margin = new Thickness(16) };
        TextBox AddField(string label, string value)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 8, 0, 3), Foreground = (Brush)Application.Current.Resources["TextMuted"] });
            var box = new TextBox { Text = value, Padding = new Thickness(6), Background = (Brush)Application.Current.Resources["BgInput"], Foreground = (Brush)Application.Current.Resources["TextPrimary"] };
            panel.Children.Add(box);
            return box;
        }
        AddField("Asset filename (from the bundled Vector set)", item.AssetPath);
        var tags = AddField("Tags (comma separated)", item.Tags);
        var opacity = AddField("Opacity (0–1)", item.Opacity.ToString("0.##"));
        var factor = AddField("Parallax factor", item.ParallaxFactor.ToString("0.####"));
        var order = AddField("Layer order", item.LayerOrder.ToString());
        var visible = new CheckBox { Content = "Visible in the compiled level", IsChecked = item.IsVisible, Margin = new Thickness(0, 10, 0, 10), Foreground = (Brush)Application.Current.Resources["TextPrimary"] };
        panel.Children.Add(visible);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
        var save = new Button { Content = "Apply", Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
        save.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(cancel); buttons.Children.Add(save); panel.Children.Add(buttons);
        dialog.Content = panel;
        if (dialog.ShowDialog() != true) return;
        if (!double.TryParse(opacity.Text, out var parsedOpacity) || !double.IsFinite(parsedOpacity) ||
            !double.TryParse(factor.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFactor) || !double.IsFinite(parsedFactor) ||
            !int.TryParse(order.Text, out var parsedOrder))
        {
            MessageBox.Show(this, "Opacity, parallax factor, or layer order has an invalid value.", "Advanced properties", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        PushUndo();
        item.Tags = tags.Text.Trim();
        item.Opacity = Math.Clamp(parsedOpacity, 0, 1);
        item.ParallaxFactor = parsedFactor;
        item.LayerOrder = parsedOrder;
        item.IsVisible = visible.IsChecked == true;
        RefreshView();
        SetStatus($"Updated advanced properties for {item.Name}.");
    }

    private void OnOpenParallaxEditor(object sender, RoutedEventArgs e)
    {
        var before = Serialize();
        _level.ParallaxLayers ??= new();
        var dialog = new Window { Title = "Parallax Layers", Width = 860, Height = 470, MinWidth = 720, MinHeight = 360, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, Background = (Brush)Application.Current.Resources["BgPanel"], Foreground = (Brush)Application.Current.Resources["TextPrimary"] };
        var dock = new DockPanel { Margin = new Thickness(12) };
        var grid = new DataGrid { AutoGenerateColumns = true, CanUserAddRows = false, IsReadOnly = false, Margin = new Thickness(0, 0, 0, 10), ItemsSource = _level.ParallaxLayers };
        grid.AutoGeneratingColumn += (_, args) => { if (args.PropertyName == nameof(LevelParallaxLayer.ImagePath)) args.Column.Header = "Texture path"; };
        DockPanel.SetDock(grid, Dock.Top); dock.Children.Add(grid);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var add = new Button { Content = "Add layer", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 6, 0) };
        add.Click += (_, _) => { _level.ParallaxLayers.Add(new LevelParallaxLayer { Name = $"Layer {_level.ParallaxLayers.Count + 1}" }); grid.ItemsSource = null; grid.ItemsSource = _level.ParallaxLayers; };
        var remove = new Button { Content = "Remove selected", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 6, 0) };
        remove.Click += (_, _) => { if (grid.SelectedItem is LevelParallaxLayer layer) _level.ParallaxLayers.Remove(layer); grid.ItemsSource = null; grid.ItemsSource = _level.ParallaxLayers; };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 6, 0), IsCancel = true };
        var apply = new Button { Content = "Apply", Padding = new Thickness(10, 6, 10, 6), IsDefault = true };
        apply.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(add); buttons.Children.Add(remove); buttons.Children.Add(cancel); buttons.Children.Add(apply);
        DockPanel.SetDock(buttons, Dock.Bottom); dock.Children.Add(buttons);
        dialog.Content = dock;
        if (dialog.ShowDialog() == true)
        {
            if (!string.Equals(before, Serialize(), StringComparison.Ordinal)) { _undo.Push(before); _redo.Clear(); }
            RenderCanvas(); SetStatus($"Updated {_level.ParallaxLayers.Count} parallax layers.");
        }
        else Restore(before);
    }

    private void OnOpenCredits(object sender, RoutedEventArgs e) => new CreditsWindow { Owner = this }.ShowDialog();

    private void OnOpenProjectManagerClick(object sender, RoutedEventArgs e) => new ProjectManagerWindow { Owner = this }.ShowDialog();
    private void OnExit(object sender, RoutedEventArgs e) => Close();
    private void OnAbout(object sender, RoutedEventArgs e) => MessageBox.Show(this, "Easytorier Level Studio\nCreate, arrange, and save parkour layouts as portable JSON levels.", "About Easytorier", MessageBoxButton.OK, MessageBoxImage.Information);
    private void UpdateTitle() => Title = $"Easytorier — {(_filePath is null ? _level.Name : Path.GetFileName(_filePath))}{(IsDirty ? " *" : "")}";
    private void SetStatus(string message) => StatusText.Text = message;
}