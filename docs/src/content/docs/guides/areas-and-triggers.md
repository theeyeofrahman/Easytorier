---
title: Areas and triggers
description: Use named Area presets and edit trigger content.
---

## Named Area presets

Place an **Area**, select it, then use **Area trigger preset** in the inspector. You can also type the exact supported preset name into the object's name field. For example, `TriggerCameraStart` exports the camera-follow trigger content using the Area's position and dimensions.

The catalog contains 48 single-trigger presets extracted from the reference object library. A matching name emits its trigger logic; arbitrary text does not invent a new trigger. Unmatched names remain native Animation Areas. Saved editable XML preserves the Area's editor identity.

## Custom trigger content

Use a Trigger object and the trigger editor for custom content supported by the game. Existing imported XML is preserved where the serializer supports it. Validate XML before export and check behavior in the target game build.

Naming an Area after a preset is an export convenience. It does not run a gameplay simulation in the editor, and presets may refer to templates or models that must exist in the target build.

## Sound feedback

**View → Interface sounds (quiet)** enables mouse-click sounds and typing ticks when editing a Trigger or Area name. These are editor feedback sounds, not level sound effects, and are off by default.
