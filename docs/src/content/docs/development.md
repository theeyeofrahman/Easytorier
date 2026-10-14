---
title: Development
description: Build Easytorier and work on its Starlight documentation site.
---

## Build the desktop app

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), clone or download the repository, and run these commands from its root:

~~~powershell
dotnet restore .\Easytorier\Easytorier.csproj
dotnet build .\Easytorier\Easytorier.csproj
dotnet run --project .\Easytorier\Easytorier.csproj
~~~

The desktop app is a WPF application targeting net10.0-windows.

## Preview the documentation

The Starlight site lives in docs/. From that directory:

~~~powershell
npm install
npm run dev
~~~

Astro prints a local URL, usually http://localhost:4321. Stop the server with **Ctrl+C**.

## Check a production build

~~~powershell
npm run build
npm run preview
~~~

The generated site is written to docs/dist/. Do not edit generated output; edit Markdown or MDX files in docs/src/content/docs/.
