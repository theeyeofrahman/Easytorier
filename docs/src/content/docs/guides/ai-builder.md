---
title: AI builder
description: Configure Gemini, review a proposed edit, and apply it as an undoable batch.
---

Open the **AI builder** tab. Enter your Gemini API key in the password field, or set `GEMINI_API_KEY`/`GOOGLE_API_KEY` before launching. Keys are sent directly to Google and are not saved in the project. Your prompt and a summary of level objects are also sent for planning.

## Generate and review

1. Choose a Gemini model ID available to your account. The initial value is `gemini-2.5-flash`; model availability depends on Google and your account.
2. Describe a concrete change, or use **Example** to fill in a beginner-route request.
3. Choose **Generate plan**. Use **Cancel** to stop waiting for a request.
4. Review the proposed actions, then apply the plan. Undo reverts the batch.

The assistant can add geometry and basic scene objects, move/resize/rotate existing objects, duplicate them, or delete content. Images/models need imported assets: AI can edit or duplicate existing ones but cannot invent missing asset files. Plans are limited to 60 actions.

## Keep the plan current

If the level changes during generation or after review, regenerate the plan. Easytorier prevents applying an outdated snapshot. Destructive plans ask for confirmation inside the editor.

Cancellation, request failures, invalid responses, and missing keys produce feedback rather than applying a partial plan. A model/API error may require changing the model ID or fixing the account/key. Live API behavior requires a working key and network connection; local request/response checks do not verify your account.
