---
title: Custom music and backgrounds
description: Import your own level music and background images, then package them for sharing.
---

Open **Level → Music & background…**. Changes are applied together and can be undone.

## Music

Choose **Import MP3…**, select a file, and set Music volume. Import validates the audio without automatically playing it. You can also enter a built-in game track name, choose **Use default music**, or choose **No music**.

MP3 is the supported custom-music format for this game workflow. The standalone **Tools → Music Player** is a separate editor listening tool and does not set the level soundtrack.

## Background

Choose **Import background…** for a PNG or JPEG. Imports are normalized to PNG. Set the background's width, height, and parallax factor; **Remove background** clears the custom background.

The background is an image object behind ordinary level objects, with collision disabled and an opaque white tint. Parallax controls its movement relative to the foreground. It can also be inspected in Scene objects. Import a background under 64 million pixels; dimensions must be between 1 and 50,000 canvas units.

## Save, share, and install

Import copies media into Easytorier's local media cache. **Save XML** copies used media into the sibling `level.assets` folder and saves relative references so the level can move between folders/computers.

Keep the XML and assets folder together. **Export game XML…** includes a companion media package and installation notes. **Play in Vector** installs used media into the chosen build's `StreamingAssets/music` and `StreamingAssets/textures` folders before launching.

Missing files are reported rather than silently omitted. Reimport missing media through this window. Your original source file is not modified.
