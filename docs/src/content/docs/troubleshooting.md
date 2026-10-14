---
title: Troubleshooting
description: Fix common setup and level export problems.
---

## Easytorier does not build

Confirm that Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) are installed, then run the build command from the repository root:

~~~powershell
dotnet build .\Easytorier\Easytorier.csproj
~~~

Read the first compiler error in the build output. Later errors can be consequences of the first one.

## A texture is missing

Check that the image exists under Easytorier/Assets/VectorTextures and that the texture packs were included in the build output. When sharing an exported XML, ensure the matching Vector texture is available to the target Vectorier installation.

## XML export reports an image without a texture

Every image object needs a Vector texture selected before it can be compiled. Select the image in the viewport and choose a texture in the inspector, then compile again.

## XML loads differently in Vectorier

Vectorier's XML format and asset names determine what can render. Check that each exported image ClassName matches a texture available to Vectorier. Confirm texture collision and object placement in the target application. Keep the JSON file so you can continue editing the level in Easytorier.

## Find the source of a problem

When reporting a bug, include the Easytorier version, Windows version, steps to reproduce, relevant JSON/XML sample with private details removed, and the full exception or error text.
