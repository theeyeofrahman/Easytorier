---
title: Troubleshooting
description: Fix common editing, export, media, AI, and progress problems.
---

## Changes are missing from the app

Save your work, close the old editor, and launch the updated build. Keep its libraries and Assets folder beside the executable. Windows may lock an executable while it is running; build to another output folder or close it before rebuilding.

## Objects will not move or transforms jump

Turn off Pan and choose Move/Resize/Scale/Rotate. Click the canvas to leave a text input. Disable Snap for continuous transforms. Right-drag, middle-drag, or Space+drag pans rather than editing objects.

Ctrl+C/V copies objects; Ctrl+Shift+C/V copies keyframes. Text input focus keeps the normal text clipboard. For group operations, check the selected count and Scene objects highlights.

## The game cannot load the level

Use Check level. Add a Player Start named `DefaultSpawn` above solid ground, or update the model's BirthSpawn reference in XML Export Settings. Game-only export requires valid model/spawn references. A library XML is not a standalone level.

Check the game's Player.log for the first error. Later repeated exceptions can be consequences of that first failure.

## Missing texture: collision

Use the updated exporter and re-export. Older projects may contain editor collision swatches as images; these now become solid platforms and are removed from texture browsing. Use a real texture for visible art.

## Floor art looks transparent

Re-export using the updated build, which converts editor ARGB colors into game RGBA. Check the object's intended opacity too. Already exported XML does not automatically change when the editor is updated.

## Custom media is missing

Keep the XML and its `.assets` folder together. Reimport missing sources through Level → Music & background. For manual game installation, copy the package's music/textures folders into StreamingAssets; Play in Vector handles this automatically.

## AI does not respond

Enter a Gemini key in the app or configure the supported environment variables. Verify the model ID is available to your account. Cancel a slow request and try a smaller prompt. Regenerate a plan after changing the level. Never include keys in bug reports or screenshots.

## Move Visualizer has no obstacle guide

Load compatible runner/moves data and game object libraries. Unknown custom binaries have no reliable obstacle definition. Known moves without a complete authored example show detectors only. Guides are references, not game collision simulation.

## Cosmetic progress will not save

Read the error in Extras. Progress belongs to your local Windows profile, not the level XML. An unreadable existing progress file is preserved. Back it up before attempting manual repair; reinstalling the executable does not create a cloud backup.

## Report a bug

Include the build used, Windows version, reproduction steps, exact error, and a minimal sample level with private media/keys removed. The desktop app requires Windows and the .NET 10 SDK to build from source.
