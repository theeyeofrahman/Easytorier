---
title: Move Visualizer
description: Inspect the rendered runner, native animation bins, and authored move references.
---

Open **Tools → Move Visualizer…** and load compatible MoveVisualizer data. It needs `runner.xml`, `moves.xml`, and Movement/Tricks `.bin` folders. The familiar Vectorier Downloads layout is detected when present; use **Load MoveVisualizer data…** for another location.

## Preview controls

Choose Movement, Tricks, or Custom bin, then select an animation. The runner is drawn from the model's capsule body figures; **Skeleton overlay** optionally displays links/nodes. Scrub the frame slider, play/pause, reverse the preview, or use Stay in place. Interpolation refreshes the preview at 60 FPS while bins retain their 20-FPS source timing.

The camera follows at a consistent scale; **Fit whole move** frames the full motion. COM trajectory and automatic guides can be toggled.

## How guides are chosen

Guides read matching move definitions and entry conditions from both `Reactions` and `ReactionGroups`. They use authored entry frames, pivot nodes, and detector lengths. Alternate entries/aliases appear in the entry selector; hover for condition details, including available corner/range rules.

Where a compact authored object-library example is available, the guide preserves its platform and trigger geometry. It is aligned to the move's entry pivot as a reference example. This is not a unique level layout or an engine collision simulation. If no complete example is found, detector guides remain available without a guessed block.

Custom binaries are matched by file contents, so a renamed copy of a known animation can retain its rules. An unknown binary has poses but no reliable environment definition; its obstacle guide is hidden rather than fabricated.

The resolver checks the configured game's object libraries and the familiar Snail Runner Downloads layout. Keep those reference files available when using authored examples.
