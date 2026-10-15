---
title: Development
description: Build the desktop app and validate its Starlight documentation.
---

## Desktop build

Install the .NET 10 SDK on Windows. From the repository root:

```powershell
dotnet restore .\Easytorier\Easytorier.csproj
dotnet build .\Easytorier\Easytorier.csproj
dotnet run --project .\Easytorier\Easytorier.csproj
```

The WPF app targets `net10.0-windows` and uses NAudio for audio. Assets are copied by the project configuration. User preferences, extras progress, and credits are stored under the local Windows profile, separately from projects.

Validate changes at their boundaries: XML save/reopen, custom-media portability, group-transform undo, AI response validation, and one-time achievement rewards. Live game behavior still needs testing in the target Vector build. Do not commit API keys or private media.

## Documentation

From `docs/`:

```powershell
npm ci
npm run build
```

Use the repository's background dev-server workflow when previewing:

```powershell
npm run dev -- --background
npm exec astro dev status
npm exec astro dev logs
npm exec astro dev stop
```

The site uses the `/Easytorier/` base path. Edit `src/content/docs/` and the sidebar in `astro.config.mjs`; generated files belong in `dist/` and must not be edited as source. A local build does not publish the site.
