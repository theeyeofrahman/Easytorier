---
title: Build a level
description: Place and edit objects with the Easytorier viewport and prefab bar.
---

## Add objects

The left **Create** sidebar contains built-in tools such as Platform, Roof, Obstacle, Trigger, Spawn, Area, Camera, Textures, Item, Model, Particle, and Animation. Choose a tool, then click in the viewport to place an object. Use the search box to filter the creation tools.

Texture prefabs appear in the horizontal bar at the bottom of the editor. Browse the bar by scrolling; drag a prefab into the viewport to place it. Texture packs are grouped by name under Easytorier/Assets/VectorTextures.

## Viewport tools

Select an object first, then choose a transform mode:

- **Select** selects objects without moving them.
- **Move** drags the selected object.
- **Resize** changes width and height by dragging.
- **Scale** changes the object's size proportionally.
- **Rotate** changes its angle by dragging.

The central viewport is resizable with the splitters around it. Drag the middle mouse button, or hold **Space** and drag, to pan. Use **Fit** to frame the canvas.

## Grid and snapping

The grid can be switched on or off with **Grid**. Choose a snap size of 8, 16, 32, or 64 units from the viewport controls. Snapping helps line up platforms and props; turn it off when you need free placement.

## Inspect an object

Select an object to edit its properties in the right inspector. Available properties include its name, position, dimensions, rotation, color, opacity, tags, visibility, parallax factor, and layer order. Texture images also expose texture-specific settings, including collision.

Use **Ctrl+D** to duplicate the selected object and **Delete** to remove it. **Ctrl+Z** and **Ctrl+Y** undo and redo editor changes.

## Texture collision

For a placed texture, enable its collision option in the inspector to request a matching collision platform when compiling to Vectorier XML. This is useful for solid floor or wall art. The collision proxy is generated during XML export; it is not a separate editable object in the canvas.

Check the exported XML in your target Vectorier workflow before relying on collision in a published level.
