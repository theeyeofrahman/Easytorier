# Easytorier documentation

Astro Starlight guides for the desktop editor, including XML saving, multi-select, block tools, AI, custom media, Area triggers, Move Visualizer, and Extras.

## Install and build

From this folder, using a Node.js version supported by the installed Astro release:

```powershell
npm ci
npm run build
```

Production output is generated in `dist/`. Edit Markdown/MDX under `src/content/docs/`, not generated HTML. The sidebar is configured in `astro.config.mjs`; the site uses the `/Easytorier/` base path.

## Preview locally

Follow this repository's background-server workflow:

```powershell
npm run dev -- --background
npm exec astro dev status
npm exec astro dev logs
npm exec astro dev stop
```

Use the URL printed by Astro, including `/Easytorier/`. A production build can also be inspected with `npm run preview`.

## Keep guides accurate

- Match button names and defaults to the desktop source.
- Distinguish editable XML saves from game-only exports.
- Explain companion assets when covering music or backgrounds.
- Describe Move Visualizer examples as references, not collision simulation.
- Do not publish API keys or machine-specific paths in examples.

Start with [Get started](src/content/docs/getting-started.md).
