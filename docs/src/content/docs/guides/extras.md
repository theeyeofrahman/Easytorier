---
title: Cosmetics and achievements
description: Earn XP, unlock cosmetics, and personalize your editor.
---

Open **Extras → Cosmetics…** or **Extras → Achievements…**. The window shows editor level, XP, achievement progress, and available rewards. Progress starts with this feature; earlier activity is not retroactively counted.

## Achievements

| Achievement | Requirement | XP | Reward |
| --- | --- | --- | --- |
| First brick | Place 1 object | 25 | Builder badge |
| Getting into it | Place 10 objects | 50 | Mint sparks trail |
| Rooftop architect | Place 100 objects | 100 | Architect badge |
| Safe and sound | Save successfully | 25 | Sunset accent |
| Good habits | Save successfully 10 times | 75 | Amber sparks trail |
| Out in the world | Export game XML | 50 | Moon badge |
| Better together | Select at least 3 objects | 35 | Ice accent |
| A little movement | Add a keyframe | 35 | Violet accent |
| Second thoughts | Undo an edit | 10 | XP |
| Route maker | Generate a parkour route | 25 | Route badge |
| Set the mood | Apply custom music/background | 40 | Rose accent |
| Creative partner | Apply an AI plan | 50 | Violet stars trail |

Each achievement awards XP once. Editor level increases every 100 XP. Small unlock notifications can be toggled under **Extras → Achievement notifications**.

## Equip cosmetics

There are 14 choices across Accent, Trail, and Badge, including defaults. A locked card states its required achievement. Choose **Equip** on an unlocked card; use **Follow editor theme**, **No trail**, or **Classic E** to restore defaults.

Accents customize editor highlights. Badges replace the E beside the app name. Cursor particles stay in the editor interface and never enter your level. Turning off **View → Interface animations** also suppresses new trail particles.

Progress and equipped choices save in `%LOCALAPPDATA%/Easytorier/extras-progress.json`, separately from levels. A backup is kept on overwrite. If existing progress cannot be read, it is preserved and the Extras window reports the problem rather than overwriting it. Progress is local, not an online leaderboard or cloud save.
