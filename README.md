# Easytorier

A Windows level studio for Vector-style parkour: build routes, animate objects, bring your own music and backgrounds, and export for Snail Runner. Made by **Zayden Rahman**, with enough ambition to give a level editor its own achievements.

## What you can do

- **Build together:** Ctrl/Shift-click or box-select objects, then move, resize, scale, rotate, copy, duplicate, or delete the group. Undo reverses the whole operation.
- **Browse prefabs:** Large texture previews, pack filters, multi-word search, saved favorites, and drag-to-place or double-click-to-select placement.
- **Arrange blocks:** Align edges, space blocks evenly, match widths/heights, generate staircases, frame surfaces, or create a parkour starter route.
- **Navigate freely:** Pan with right-drag, middle-drag, Space+drag, or the Pan toggle. Ctrl+wheel zooms around the pointer. Snapping is optional.
- **Animate:** Keyframes, easing, Auto Key, motion paths, onion skin, and animation-clip import/export. Object copy/paste preserves animation and collision settings.
- **Use named Areas:** Choose a trigger preset such as `TriggerCameraStart`; supported presets export their trigger logic using your Area bounds.
- **Set the atmosphere:** Import MP3 music and PNG/JPEG backgrounds; adjust music volume, background size, and parallax. Media travels in a companion assets folder.
- **Build with Gemini:** Generate a proposed edit, review it, then apply it as one undoable batch. Choose a model, cancel requests, and regenerate stale plans.
- **Inspect moves:** The Move Visualizer renders the runner body, animation bins, detector guides, and available authored obstacle examples.
- **Personalize the studio:** Themes, quiet click/typing sounds, optional interface animations, Zephyr, a music player, and Arcade Rules tools.
- **Collect extras:** 12 achievements award XP and unlock accent colors, badges, and cursor trails. Progress and equipped cosmetics persist locally.

## Start building

1. Open the level editor or create a project in Project Manager.
2. Drag in platforms and textures. Add a **Player Start** named `DefaultSpawn` above solid ground.
3. Select objects to transform them. Enable **Solid collision under this image** when floor art should be solid.
4. Choose **Level → Music & background** for custom media.
5. **Save XML** keeps the editable level, including animation and editor metadata. Older JSON projects still open.
6. Use **Check level**, then **Export game XML…** for game-only XML, or **Play in Vector** with a compatible Snail Runner build.

Keep `name.xml` together with `name.assets/` when moving a level with custom media. A successful XML check does not replace testing the level in the game. Editor animation keyframes are preserved in saved projects; they are not automatically converted into native character `.bin` animations.

## Build and run

Requires Windows and the **.NET 10 SDK** to build. Easytorier uses WPF and targets `net10.0-windows`.

```powershell
dotnet build .\Easytorier\Easytorier.csproj
dotnet run --project .\Easytorier\Easytorier.csproj
```

Keep a built executable beside its accompanying files and `Assets` folder. Move Visualizer also needs compatible `runner.xml`, `moves.xml`, animation bins, and object libraries for authored obstacle examples.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| Ctrl+N / Ctrl+O / Ctrl+S | New / open / save XML |
| Ctrl+A | Select all visible objects |
| Ctrl+C / Ctrl+V | Copy / paste selected objects |
| Ctrl+D / Delete | Duplicate / delete selection |
| Ctrl+Z / Ctrl+Y | Undo / redo |
| Ctrl+Shift+C / Ctrl+Shift+V | Copy / paste current keyframe |
| 1–5 | Select / Move / Resize / Scale / Rotate |
| Right-drag, middle-drag, or Space+drag | Pan |
| Ctrl+wheel | Zoom around the pointer |
| F1 | Show a tip |

Text inputs retain their normal typing and clipboard behavior. Click the canvas before using editor shortcuts.

## Guides

Read the [Starlight guide](https://theeyeofrahman.github.io/Easytorier/) or the [guide source and build instructions](docs/README.md). Local documentation changes appear on the hosted site after its deployment workflow runs.

## Credits

- **Zayden Rahman — Creator, lead programmer, and certified rooftop mastermind.** The vision, the persistence, the glorious “what if we added…” energy: Zayden is the driving force behind Easytorier's evolution into a creative studio. Platforms get placed; standards get raised.
- **Ghosted** — Programming help.
- **DC** — Programmer's close friend.

## Ownership and attribution

Easytorier is independent. Vector belongs to Nekki; Vectorier belongs to DoritoTheChips and Sol. Easytorier is not affiliated with them. Game assets and imported media retain their owners' rights.
