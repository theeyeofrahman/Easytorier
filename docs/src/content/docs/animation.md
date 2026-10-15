---
title: Animation
description: Keyframes, easing, animation clips, and editor playback.
---

Open **Dynamic editor** to animate level objects. This timeline is separate from the native character animations shown in [Move Visualizer](/Easytorier/guides/move-visualizer/).

## Keyframes and playback

Select an object, choose a frame, and add a keyframe. Keys contain position, size, rotation, and color. Add another pose later in the timeline, then play the preview. Set the total frame count and FPS to control timing.

Easing options are Linear, Ease In, Ease Out, and Custom. Custom easing exposes support-point values. Timeline keys can be moved, and keyframe navigation jumps between poses.

**Auto Key** records transformations at the current frame. With Auto Key off, completed object/group transforms also update the object's authored animation data so edits survive saving. Onion skin shows nearby poses; motion paths visualize travel.

## Copy and share

- Ctrl+Shift+C copies the current keyframe; Ctrl+Shift+V pastes it onto the selected object at the current frame.
- Ctrl+C/V copies entire objects with their animation data. Ctrl+D duplicates the selection.
- **File → Export Animation Clip…** writes an `.eanim.json` clip. **Import Animation Clip…** applies a clip to a selected object.

Save XML retains keyframes in the editor snapshot. Game-only XML export does not automatically turn editor keyframes into native `.bin` character animations.
