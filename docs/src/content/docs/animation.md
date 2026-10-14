---
title: Animation
description: Animate level object transforms with the Dynamic Editor timeline.
---

The **Dynamic Editor** is a separate tab in the main editor. It provides a timeline for editing and previewing object keyframes.

## Work with keyframes

Select an object and add keyframes at the frames where its pose should change. A keyframe stores position, size, rotation, and color. Move keyframes along the timeline to adjust when a change happens. The level document also stores the timeline frame count and frames per second.

Choose an easing preset for a keyframe:

- **Linear** moves at a steady rate.
- **Ease In** starts gently and speeds up.
- **Ease Out** slows toward the next pose.
- **Custom** uses support-point values for a custom curve.

Use timeline playback to preview the motion. Save the level as JSON to keep its animation data.

## Tips

- Add a keyframe at the starting pose and another at the destination.
- Keep keyframes on the same object when animating that object's movement.
- Preview after changing timing or easing.
- XML export is intended for Vectorier level content; check the output in Vectorier to confirm that the target format supports the animation data you need.
