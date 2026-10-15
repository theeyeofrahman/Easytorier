---
title: Save, import, and export
description: Editable XML saves, legacy JSON, companion media, and Snail Runner playtesting.
---

## Save XML

**Save XML**, **File → Save**, and Ctrl+S save editable XML. An XML comment preserves Easytorier's keyframes, IDs, hidden objects, and editor metadata alongside the level XML. Keep this comment to retain the complete editing state.

Saving is allowed before the level is playable. Saving over an existing file keeps a `.bak` of the previous version. New, Open, and Close offer Save/Discard/Cancel when there are changes.

**File → Open** reads saved XML, ordinary level XML, and legacy JSON. Saving an opened JSON project creates an XML counterpart. If the game XML has been edited externally, Easytorier imports those edits instead of restoring an outdated embedded snapshot; editor-only data may consequently be unavailable.

## Import and library exports

**Import Vectorier XML…** offers level import/append options. Existing source XML is preserved where supported. Objects and Buildings library exports are reusable definitions rather than standalone playable levels. Use Import for these library formats.

## Export game XML

**Export game XML…** creates game-only XML without the editor snapshot. **File → Compile to Vectorier XML…** reaches the same level-export workflow. Only visible objects are exported, grouped by parallax factor.

Export checks player model/spawn references. Images require real texture names. Editor-only `collision`, `collision50`, and `collision100` swatches from older projects export as platforms rather than missing game sprites.

Custom media is packaged into `level.assets/music/` and `level.assets/textures/`, with installation instructions. Copy those folders into the game's `Vector_Data/StreamingAssets/`, and put the level XML in `StreamingAssets/xmlroot/levels/`. Keep the assets folder with the editable XML when sharing or reopening it elsewhere.

## Check and play

**Check level** checks export readiness and reports missing player setup or solid ground. It does not run game physics or verify every installed texture.

**Play in Vector** remembers your selected `Vector.exe`, exports into its StreamingAssets level folder, installs custom media, and launches the selected level. Debug options include platforms, triggers, Areas, detectors, hunter mode, and debug-UI visibility. It really starts the game; use Check level when you only need validation.

Easytorier timeline keyframes are retained in editable saves and animation clips. They are not automatically compiled into native character `.bin` motion or every native game animation feature.
