---
title: Texture library
description: Browse texture packs, save favorites, and export solid geometry correctly.
---

Bundled textures live under `Assets/VectorTextures` beside the application. Packs are grouped by folder. The prefab shelf offers pack filtering, multi-word search, saved favorites, larger previews, size labels, and tooltips. Drag its upper divider to resize it; choose Load more prefabs to browse additional matches.

Drag a texture onto the canvas, or double-click a prefab and click to place. New textures use original sizing, fitting very large images within 512 canvas units. Existing object sizes are retained. **Edit → Resize selected textures to normal size** reapplies normal sizing to selected images and supports Undo.

Use the inspector to set dimensions, rotation, tint, opacity, depth, and image type. Enable **Solid collision under this image** for floor/wall artwork. The proxy uses the rotated bounding rectangle; precise slopes may need separate collision geometry.

Editor-only collision swatches are excluded from the texture browser. Old image objects using `collision`, `collision50`, or `collision100` export as Platform geometry, preventing the game's missing-texture error.

Save editable XML, then export for the game. A texture's filename supplies its game class name, so bundled game textures still need to exist in the target build. For your own background art, use [Level music & background](/Easytorier/guides/level-media/) to package the image with the level.
