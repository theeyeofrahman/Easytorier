---
title: Build a level
description: Place prefabs, transform selections, arrange blocks, and navigate the canvas.
---

## Prefab shelf

Use the pack selector and multi-word search to find textures or built-in objects. Select an item and choose **Favorite / unfavorite** to save it; **Favorites only** filters the shelf. **Clear** resets the search, and **Load more prefabs** expands the current batch.

Drag a prefab onto the canvas, or double-click it to select a placement tool and click to place. The shelf is resizable with the horizontal divider above it. Textures use original sizing, with very large images fitted within 512 canvas units; the previous 100-PPU placement mode is no longer active.

## Select and transform

- Click an object to select it; in Select mode, dragging it moves it.
- Ctrl/Shift-click toggles objects on the canvas. In **Scene objects**, Ctrl-click toggles and Shift-click selects a range.
- Drag empty canvas to box-select intersecting visible objects. Hold Ctrl/Shift to add to the existing selection.
- **Ctrl+A** selects all visible objects. **Edit → Clear selection** deselects them.

Choose **Move**, **Resize**, **Scale**, or **Rotate** to transform the selection. Group scale/rotation preserves the arrangement around the group. Copy, paste, duplicate, and delete also operate on the selection. Undo reverses a group operation as one edit. The inspector shows the active object's properties; it is not a batch property editor.

**Ctrl+C/V** copies and pastes objects, including their keyframes and collision settings. Pasted objects get new IDs and a small position offset.

## Navigate and snap

Pan using right-drag, middle-drag, Space+left-drag, or the **Pan** toggle. Switch to an editing tool to leave Pan mode. Ctrl+wheel zooms around the pointer; **Fit** frames the canvas.

**Grid** controls visibility. **Snap** is off by default for continuous transforms; enable it for 8, 16, 32, or 64-unit alignment. Rotation snaps to 15-degree steps when enabled.

## Block tools

Select blocks in Scene objects, then open **Quick build → Block tools** or **Plugins → Block tools**:

- Align left, right, top, or bottom edges.
- Space at least three blocks evenly horizontally or vertically.
- Match widths or heights to the first block in the selection's object order.
- Build five stairs, each rising 32 units, from the selected starting block.

**Surface Kit** builds floor/wall/corner arrangements around selected textures. **Parkour Route Starter** generates a short route. These operations support Undo.

## Collision and visuals

Enable **Solid collision under this image** to export a Platform proxy. Rotated images use an axis-aligned bounding box, so the proxy may extend beyond sloped art. Use separate platform geometry for precise collision layouts.

Bounds, pivots, collision proxies, onion skin, and motion paths can be toggled in the viewport. Colors entered as `#AARRGGBB` are converted to the game's `#RRGGBBAA` during export; intentional opacity is retained.
