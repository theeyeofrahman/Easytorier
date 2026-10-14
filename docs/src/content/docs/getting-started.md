---
title: Get started
description: Install, launch, and create your first Easytorier level.
---

Easytorier runs as a Windows desktop app. You can use a built release or build the source with the .NET SDK.

## Requirements

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for building from source

## Build and launch from source

From the repository root, run:

~~~powershell
dotnet build .\Easytorier\Easytorier.csproj
dotnet run --project .\Easytorier\Easytorier.csproj
~~~

To launch a previously built Debug executable:

~~~powershell
.\Easytorier\bin\Debug\net10.0-windows\Easytorier.exe
~~~

## Choose a starting point

The welcome screen offers:

- **Start Level Editor** to open a blank level.
- **Project Manager** to create or open a project workspace.
- **Theme Editor** to customize the app colors.
- **Arcade Rules** to edit minigame rules.
- **Credits** to view and edit contributor credits.

For a level you plan to keep developing, create a project first. A workspace keeps its level file and description together.

## Make a first level

1. Start the editor or open a project.
2. Choose a built-in object from the left **Create** palette, or drag a texture prefab from the bar at the bottom onto the viewport.
3. Select the placed object. Adjust its properties in the inspector on the right.
4. Use the viewport toolbar to select, move, resize, scale, or rotate it.
5. Save the editable level with **File → Save** or **Ctrl+S**.
6. Choose **Compile to Vectorier XML** when you want an XML output for Vectorier.

Easytorier saves level files as JSON for continued editing. XML is a separate compiled output.
