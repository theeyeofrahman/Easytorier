# Easytorier

Easytorier is a Windows desktop level editor for building Vector-style parkour levels. Arrange platforms and props on a canvas, animate objects on a timeline, and export levels as Vectorier XML.

## Highlights

- **Level editor:** Place built-in objects and texture prefabs, drag prefabs onto the canvas, and edit object position, size, rotation, color, visibility, tags, opacity, and layer order.
- **Viewport tools:** Select, move, resize, scale, and rotate objects. Pan with the middle mouse button or hold **Space** and drag. Use **Fit** to frame the canvas.
- **Grid and snapping:** Toggle the grid and choose 8, 16, 32, or 64 unit snapping for object placement and transforms.
- **Animation:** Add and move keyframes, preview playback, adjust easing, and inspect animated poses in the Dynamic Editor tab.
- **Texture collision:** Mark an image as solid to compile a matching platform collision proxy.
- **Vectorier XML:** Import existing Vectorier level XML and compile the current level to XML.
- **Project Manager:** Search, create, rename, duplicate, and delete project workspaces; edit descriptions and thumbnails; open a project in the editor; open its folder; or export it as a ZIP package.
- **Customization:** Edit and save themes, and manage the credits list from the app.

## Requirements

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

The app uses WPF and targets `net10.0-windows`.

## Build and run

From the repository root, run:

```powershell
dotnet build .\Easytorier\Easytorier.csproj
dotnet run --project .\Easytorier\Easytorier.csproj
```

To launch a built executable directly, run:

```powershell
.\Easytorier\bin\Debug\net10.0-windows\Easytorier.exe
```

## Getting started

1. Choose **Start Level Editor** to begin with an empty level, or open **Project Manager** to create or select a workspace.
2. Use the prefab bar to add objects. Drag a prefab onto the viewport, or select a tool and click to place an object.
3. Select an object to edit its properties in the inspector. Use the timeline controls in the **Dynamic Editor** tab for animation.
4. Save a standalone level as JSON, or choose **Compile to Vectorier XML** when you are ready to export.

Project workspaces are stored in the app's `Projects` folder beside the running executable. Each project contains a `description.txt` and a `level.json`; thumbnails are stored as `thumbnail.png`, `thumbnail.jpg`, or `thumbnail.jpeg`. Credits are saved in the current user's local Easytorier application data.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| Ctrl+N | New level |
| Ctrl+O | Open level |
| Ctrl+S | Save level |
| Ctrl+Z / Ctrl+Y | Undo / redo |
| Ctrl+D | Duplicate selected object |
| Delete | Delete selected object |
| 1–5 | Select, Move, Resize, Scale, or Rotate tool |
| Space + drag | Pan the viewport |
| Middle mouse drag | Pan the viewport |

## Credits

- **Zayden Rahman** — Main programmer and project lead, shaping the core editor and growing Easytorier into a more capable level studio.
- **Ghosted** — Programming help.
- **DC** — Programmer's close friend.

## Ownership and attribution

Easytorier is an independent project. Vector is owned by Nekki, and the rights to Vectorier belong to DoritoTheChips and Sol. Easytorier is not affiliated with them.
