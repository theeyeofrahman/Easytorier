---
title: Projects
description: Organize levels, notes, thumbnails, and media in workspaces.
---

Open Project Manager from the welcome screen or **Project** menu. Create, search, rename, duplicate, and delete workspaces; double-click a project or choose **Edit Project** to open it. The built-in Vector workspace is protected from rename/delete.

Project descriptions and optional thumbnails help identify a workspace. Thumbnail names can be `thumbnail.png`, `thumbnail.jpg`, or `thumbnail.jpeg`. **Open Folder** opens the workspace in Explorer; **Export ZIP** packages it.

## Files to keep

Workspaces live in `Projects` beside the running executable. New projects use `level.xml`; older ones may contain `level.json`. The manager prefers XML when both exist. A typical project contains:

```text
My Project/
  level.xml
  level.assets/
    music/
    textures/
  description.txt
  thumbnail.png
```

The assets folder is only needed when the level uses custom media. Move/back up the whole workspace rather than its XML alone. A successful overwrite creates a `.bak` of the previous save.

Achievements, cosmetic equipment, themes, favorites, preferences, and credits belong to the Windows user's editor profile; they are separate from project files.
