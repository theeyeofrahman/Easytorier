using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Easytorier;

public sealed class VectorTextureAsset
{
    private static readonly Dictionary<string, BitmapImage> BitmapCache = new(StringComparer.OrdinalIgnoreCase);

    private BitmapImage? _preview;
    private (int Width, int Height)? _nativeSize;

    public string FileName { get; init; } = "";
    public string DisplayName => Path.GetFileNameWithoutExtension(FileName);
    public string PackName { get; init; } = "CORE TEXTURE PACK";
    public string FullPath { get; init; } = "";
    public int NativeWidth => (_nativeSize ??= ReadImageSize(FullPath)).Width;
    public int NativeHeight => (_nativeSize ??= ReadImageSize(FullPath)).Height;
    public BitmapImage Preview => _preview ??= LoadBitmapFile(FullPath, 96);

    public static string AssetDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "VectorTextures");

    public static IReadOnlyList<VectorTextureAsset> LoadAll()
    {
        if (!Directory.Exists(AssetDirectory)) return Array.Empty<VectorTextureAsset>();
        return Directory.EnumerateFiles(AssetDirectory, "*", SearchOption.AllDirectories)
            .Where(IsSupportedImage)
            .Select(path =>
            {
                var relativePath = Path.GetRelativePath(AssetDirectory, path).Replace(Path.DirectorySeparatorChar, '/');
                return new VectorTextureAsset
                {
                    FileName = relativePath,
                    PackName = Path.GetFileName(Path.GetDirectoryName(path)) ?? "CORE TEXTURE PACK",
                    FullPath = path
                };
            })
            .OrderBy(texture => texture.PackName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(texture => texture.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsSupportedImage(string path) =>
        Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".jpeg", StringComparison.OrdinalIgnoreCase);

    public static string? ResolvePath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        if (Path.IsPathRooted(fileName) && File.Exists(fileName)) return fileName;

        var root = Path.GetFullPath(AssetDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var relativePath = fileName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(AssetDirectory, relativePath));
        if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(path)) return path;

        var legacyPath = Directory.EnumerateFiles(AssetDirectory, Path.GetFileName(fileName), SearchOption.AllDirectories).FirstOrDefault();
        return legacyPath;
    }

    public static BitmapImage? LoadBitmap(string? fileName)
    {
        var path = ResolvePath(fileName);
        if (path is null) return null;
        if (BitmapCache.TryGetValue(path, out var cached)) return cached;
        var bitmap = LoadBitmapFile(path);
        BitmapCache[path] = bitmap;
        return bitmap;
    }

    private static BitmapImage LoadBitmapFile(string path, int? decodeWidth = null)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = File.OpenRead(path);
        if (decodeWidth.HasValue) bitmap.DecodePixelWidth = decodeWidth.Value;
        bitmap.EndInit();
        bitmap.StreamSource?.Dispose();
        bitmap.Freeze();
        return bitmap;
    }

    public static (int Width, int Height) ReadImageSize(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        return (Math.Max(1, frame.PixelWidth), Math.Max(1, frame.PixelHeight));
    }
}

public partial class VectorTexturePickerWindow : Window
{
    private IReadOnlyList<VectorTextureAsset> _allTextures = Array.Empty<VectorTextureAsset>();

    public VectorTextureAsset? SelectedTexture { get; private set; }

    public VectorTexturePickerWindow()
    {
        InitializeComponent();
        _allTextures = VectorTextureAsset.LoadAll();
        PackFilterBox.ItemsSource = new[] { "ALL TEXTURE PACKS" }
            .Concat(_allTextures.Select(texture => texture.PackName).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        PackFilterBox.SelectedIndex = 0;
        ApplyPackFilter();
    }

    private void OnPackFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyPackFilter();

    private void ApplyPackFilter()
    {
        if (PackFilterBox.SelectedItem is not string selectedPack) return;
        var filtered = selectedPack == "ALL TEXTURE PACKS"
            ? _allTextures
            : _allTextures.Where(texture => string.Equals(texture.PackName, selectedPack, StringComparison.OrdinalIgnoreCase)).ToArray();
        TextureList.ItemsSource = filtered;
        if (TextureList.Items.Count > 0) TextureList.SelectedIndex = 0;
    }

    private void OnTextureSelected(object sender, SelectionChangedEventArgs e)
    {
        if (TextureList.SelectedItem is not VectorTextureAsset texture) return;
        PreviewImage.Source = texture.Preview;
        NameLabel.Text = texture.FileName;
        SizeLabel.Text = $"{texture.PackName}  •  {texture.NativeWidth} × {texture.NativeHeight} px";
    }

    private void OnTextureDoubleClick(object sender, MouseButtonEventArgs e) => OnUseTexture(sender, e);

    private void OnUseTexture(object sender, RoutedEventArgs e)
    {
        if (TextureList.SelectedItem is not VectorTextureAsset texture) return;
        SelectedTexture = texture;
        DialogResult = true;
    }
}