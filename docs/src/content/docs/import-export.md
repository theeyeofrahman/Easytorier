---
title: Import and export
description: Save editable JSON levels and compile Vectorier XML.
---

Easytorier uses JSON for editable work and Vectorier XML as a compiled interchange format.

## Save and reopen an editable level

Use **File → Save** or **Ctrl+S** to save the current level. Use **Save As** to choose a file and **File → Open** or **Ctrl+O** to reopen a JSON level. The project workflow keeps the editable file in the project's level.json.

JSON retains Easytorier editing data such as object properties, animation keyframes, and parallax layers.

## Import Vectorier XML

Choose **File → Import Vectorier XML** and select a level XML file. Imported objects are added to a level document for editing. The importer reads the Vectorier Track object groups, positions, dimensions, image class names, image metadata, and some static properties. It also preserves source XML on imported objects where available.

Imported files can use Vectorier-specific features that Easytorier does not expose as editable controls. Review the scene after import and verify the compiled result in Vectorier.

## Compile to Vectorier XML

Choose **Compile to Vectorier XML** from the toolbar or **File** menu and select an output location. Easytorier writes a Vectorier-style XML tree with a Root and Track, grouping visible objects by parallax factor.

Image objects need a Vector texture selected; the XML ClassName is based on the texture filename. Keep the texture assets available to Vectorier at the destination. If an image has texture collision enabled, the exporter writes its collision representation alongside the image.

## Before sharing

1. Save the editable JSON source.
2. Compile XML to a separate output file.
3. Check image names and asset availability in the target Vectorier setup.
4. Open or test the XML in Vectorier before publishing.
