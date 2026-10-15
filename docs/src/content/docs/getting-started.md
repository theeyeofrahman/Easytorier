---
title: Get started
description: Build your first level, save XML, and prepare it for Snail Runner.
---

Easytorier is a Windows WPF app. To build from source, install the .NET 10 SDK and run these commands from the repository root:

```powershell
dotnet build .\Easytorier\Easytorier.csproj
dotnet run --project .\Easytorier\Easytorier.csproj
```

When using a built copy, keep its executable, libraries, and Assets folder together.

## Your first route

1. Open **Start Level Editor** or choose a workspace in **Project Manager**.
2. Drag a Platform from the prefab shelf onto the canvas. Newly placed objects are immediately editable.
3. Add a **Player Start** named `DefaultSpawn` above the platform. The default player models reference this exact name.
4. Add textures and enable collision for solid floor/wall images. Decorative images can remain non-solid.
5. Select, move, resize, scale, or rotate objects. Ctrl/Shift-click adds to your selection; dragging empty canvas box-selects.
6. Choose **Save XML**. Saving preserves editor settings and keyframes, even for an unfinished level. Older JSON files can still be opened.
7. Run **Check level** before exporting or playing. Choose **Export game XML…** or configure **Play in Vector** with a compatible game build.

## Find the other features

- **Level → Music & background** imports custom media.
- **AI builder** generates reviewed editing plans using your Gemini key.
- **Quick build → Block tools** arranges selected blocks.
- **Tools → Move Visualizer** previews the runner and animation bins.
- **Extras** contains achievements and cosmetics.
- **View** controls themes, interface sounds, and interface animations.

See [building levels](/Easytorier/building-levels/), [files and export](/Easytorier/import-export/), and [custom media](/Easytorier/guides/level-media/).
