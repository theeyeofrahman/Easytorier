using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Easytorier;

public static class VectorierXmlSerializer
{
    public const string TextureCollisionGroupName = "EasytorierTextureCollision";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Export(LevelDocument level)
    {
        var root = new XElement("Root");
        var track = new XElement("Track");
        root.Add(track);

        foreach (var layer in level.Objects.Where(o => o.IsVisible).GroupBy(o => SafeFactor(o.ParallaxFactor))
                     .OrderBy(g => double.Parse(g.Key, Invariant)))
        {
            var layerElement = new XElement("Object", new XAttribute("Factor", layer.Key));
            var content = new XElement("Content");
            foreach (var item in layer.OrderBy(o => o.LayerOrder).ThenBy(o => o.Y).ThenBy(o => o.X))
                content.Add(WriteObject(item));
            if (content.HasElements)
            {
                layerElement.Add(content);
                track.Add(layerElement);
            }
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineChars = "\n", Encoding = new System.Text.UTF8Encoding(false) };
        using var stream = new MemoryStream();
        using (var xml = XmlWriter.Create(stream, settings)) doc.Save(xml);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    public static LevelDocument Import(string path)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var reader = XmlReader.Create(path, settings);
        var doc = XDocument.Load(reader, LoadOptions.None);
        var level = new LevelDocument { Name = Path.GetFileNameWithoutExtension(path) };
        foreach (var group in doc.Descendants("Track").Elements("Object"))
        {
            var factor = Number((string?)group.Attribute("Factor"), 1);
            foreach (var node in group.Element("Content")?.Elements() ?? Enumerable.Empty<XElement>())
                ReadNode(node, 0, 0, factor);
        }

        void ReadNode(XElement node, double parentX, double parentY, double factor)
        {
            var nodeX = Number((string?)node.Attribute("X"), 0);
            var nodeY = Number((string?)node.Attribute("Y"), 0);
            if (node.Name.LocalName == "Object" && node.Element("Content") is XElement nested)
            {
                if (string.Equals((string?)node.Attribute("Name"), TextureCollisionGroupName, StringComparison.Ordinal)
                    && nested.Element("Image") is XElement collidableImage)
                {
                    var previousCount = level.Objects.Count;
                    ReadNode(collidableImage, parentX + nodeX, parentY + nodeY, factor);
                    if (level.Objects.Count > previousCount
                        && nested.Elements("Platform").Any())
                        level.Objects[^1].TextureCollisionEnabled = true;
                    return;
                }

                foreach (var child in nested.Elements())
                    ReadNode(child, parentX + nodeX, parentY + nodeY, factor);
                return;
            }

            var kind = KindFromTag(node.Name.LocalName);
            var asset = (string?)node.Attribute("ClassName") ?? "";
            var width = Math.Max(1, Number((string?)node.Attribute("Width"), 128));
            var height = Math.Max(1, Number((string?)node.Attribute("Height"), 24));
            var matrix = node.Descendants("Matrix").FirstOrDefault();
            var importedRotation = matrix is null ? 0 : Math.Atan2(Number((string?)matrix.Attribute("B"), 0), Number((string?)matrix.Attribute("A"), width)) * 180.0 / Math.PI;
            var item = new LevelObject
            {
                Kind = kind,
                Name = (string?)node.Attribute("Name") ?? (asset.Length > 0 ? asset : node.Name.LocalName),
                X = parentX + nodeX,
                Y = parentY + nodeY,
                Width = width,
                Height = height,
                Rotation = importedRotation,
                AssetPath = asset.Length == 0 ? "" : asset + ".png",
                ImageType = node.Name.LocalName == "Image" ? (int)Number((string?)node.Attribute("Type"), 0) : 0,
                ImageDepth = node.Name.LocalName == "Image" ? ((string?)node.Attribute("Depth") switch { "0" => 0, "1" => 2, _ => 1 }) : 1,
                ParallaxFactor = factor,
                LegacyXml = node.ToString(SaveOptions.DisableFormatting)
            };
            item.NativeWidth = (int)Number((string?)node.Attribute("NativeX"), item.Width);
            item.NativeHeight = (int)Number((string?)node.Attribute("NativeY"), item.Height);
            var triggerContent = node.Element("Content");
            if (triggerContent is not null) item.TriggerContentXml = triggerContent.ToString(SaveOptions.DisableFormatting);
            level.Objects.Add(item);
        }
        return level;
    }

    private static XElement WriteObject(LevelObject item)
    {
        var tag = TagFromKind(item.Kind);
        var node = new XElement(tag,
            new XAttribute("X", F(item.X)),
            new XAttribute("Y", F(item.Y)));

        if (item.Kind is "Image" or "Platform" or "Obstacle" or "Roof" or "Trapezoid" or "Trigger" or "Area")
        {
            node.Add(new XAttribute("Width", F(item.Width)), new XAttribute("Height", F(item.Height)));
        }

        if (item.Kind == "Image")
        {
            var className = Path.GetFileNameWithoutExtension(item.AssetPath);
            if (string.IsNullOrWhiteSpace(className))
                throw new InvalidDataException($"Image object '{item.Name}' has no Vector texture selected.");
            node.Add(new XAttribute("Name", item.Name), new XAttribute("ClassName", className),
                new XAttribute("NativeX", Math.Max(1, item.NativeWidth).ToString(Invariant)),
                new XAttribute("NativeY", Math.Max(1, item.NativeHeight).ToString(Invariant)));
            if (item.ImageType is > 0 and <= 3) node.Add(new XAttribute("Type", item.ImageType.ToString(Invariant)));
            if (item.ImageDepth == 0) node.Add(new XAttribute("Depth", "0"));
            else if (item.ImageDepth == 2) node.Add(new XAttribute("Depth", "1"));
        }
        else if (!string.IsNullOrWhiteSpace(item.Name))
        {
            node.Add(new XAttribute("Name", item.Name));
        }

        var properties = new XElement("Properties");
        var stat = new XElement("Static");
        if (item.Kind == "Image" && Math.Abs(item.Rotation) > 0.001)
        {
            var radians = item.Rotation * Math.PI / 180.0;
            var a = Math.Cos(radians) * item.Width;
            var b = Math.Sin(radians) * item.Width;
            var c = -Math.Sin(radians) * item.Height;
            var d = Math.Cos(radians) * item.Height;
            stat.Add(new XElement("Matrix",
                new XAttribute("A", F(a)), new XAttribute("B", F(b)),
                new XAttribute("C", F(c)), new XAttribute("D", F(d)),
                new XAttribute("Tx", "0"), new XAttribute("Ty", "0")));
        }
        if (!string.IsNullOrWhiteSpace(item.ColorHex) && item.ColorHex != "#FFFFFFFF")
            stat.Add(new XElement("StartColor", new XAttribute("Color", item.ColorHex)));
        if (item.Kind == "Platform" || item.Kind == "Obstacle" || item.Kind == "Roof")
            stat.Add(new XElement("Selection", new XAttribute("Choice", "AITriggers"), new XAttribute("Variant", "CommonMode")));
        if (stat.HasElements)
        {
            properties.Add(stat);
            node.Add(properties);
        }

        if (item.Kind == "Trigger" && !string.IsNullOrWhiteSpace(item.TriggerContentXml))
        {
            try
            {
                var trigger = XElement.Parse(item.TriggerContentXml);
                node.Add(trigger.Name.LocalName == "Content" ? trigger : new XElement("Content", trigger));
            }
            catch (XmlException ex) { throw new InvalidDataException($"Trigger '{item.Name}' contains invalid XML: {ex.Message}", ex); }
        }
        if (item.Kind == "Image" && item.TextureCollisionEnabled)
        {
            var bounds = RotatedBounds(item.X, item.Y, item.Width, item.Height, item.Rotation);
            var collision = new XElement("Platform",
                new XAttribute("X", F(bounds.X)),
                new XAttribute("Y", F(bounds.Y)),
                new XAttribute("Width", F(bounds.Width)),
                new XAttribute("Height", F(bounds.Height)),
                new XAttribute("Name", $"{item.Name} Collision"),
                new XElement("Properties", new XElement("Static",
                    new XElement("Selection", new XAttribute("Choice", "AITriggers"), new XAttribute("Variant", "CommonMode")))));
            return new XElement("Object",
                new XAttribute("Name", TextureCollisionGroupName),
                new XAttribute("X", "0"),
                new XAttribute("Y", "0"),
                new XElement("Content", node, collision));
        }

        return node;
    }

    private static string TagFromKind(string kind) => kind switch
    {
        "Image" => "Image", "Trapezoid" or "Roof" => "Trapezoid", "Trigger" => "Trigger",
        "Area" => "Area", "Camera" => "Camera", "Spawn" => "Spawn", "Item" => "Item",
        "Model" => "Model", "Particle" => "Particle", "Animation" => "Animation", "Object" => "Object",
        _ => "Platform"
    };

    private static string KindFromTag(string tag) => tag switch
    {
        "Image" => "Image", "Trapezoid" => "Trapezoid", "Trigger" => "Trigger", "Area" => "Area",
        "Camera" => "Camera", "Spawn" => "Spawn", "Item" => "Item", "Model" => "Model",
        "Particle" => "Particle", "Animation" => "Animation", "Object" => "Object", _ => "Platform"
    };

    public static (double X, double Y, double Width, double Height) RotatedBounds(double x, double y, double width, double height, double rotation)
    {
        var radians = rotation * Math.PI / 180.0;
        var cos = Math.Abs(Math.Cos(radians));
        var sin = Math.Abs(Math.Sin(radians));
        var boundsWidth = width * cos + height * sin;
        var boundsHeight = width * sin + height * cos;
        var centerX = x + width / 2;
        var centerY = y + height / 2;
        return (centerX - boundsWidth / 2, centerY - boundsHeight / 2, boundsWidth, boundsHeight);
    }

    private static string SafeFactor(double value) => (double.IsFinite(value) ? value : 1).ToString("0.####", Invariant);
    private static string F(double value) => (double.IsFinite(value) ? value : 0).ToString("0.######", Invariant);
    private static double Number(string? value, double fallback) =>
        double.TryParse(value, NumberStyles.Float, Invariant, out var result) && double.IsFinite(result) ? result : fallback;
}