---
title: Texture library
description: Find and use Easytorier's bundled Vector texture packs.
---

## Where textures live

The app's bundled library is in Easytorier/Assets/VectorTextures. Texture files are grouped into folders named for their packs, such as DUSK TEXTURE PACK, NIGHT TEXTURE PACK, and V_ TEXTURE PACK.

The editor loads the bundled images into the prefab browser. Use its search/filter controls to find an asset, then drag the prefab onto the viewport.

## Use a texture in a level

1. Drag a texture prefab from the bottom bar into the viewport.
2. Select the placed image and adjust its size, rotation, opacity, depth, or other inspector settings.
3. Enable texture collision when that image should generate a collision proxy during XML compilation.
4. Save the editable JSON and compile the Vectorier XML.

For an exported image, the texture filename supplies the Vectorier class name. Keep the corresponding texture available to Vectorier when you move the XML to another installation.
