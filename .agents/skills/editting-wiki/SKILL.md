---
name: editting-wiki
description: Maintaining and editting the wiki, only do when explicitly asked to edit the wiki by user
metadata:
  internal: true
---

Wiki located in ../Brigade.NET.wiki/

Always load and use the deslop skill for wiki

# Code blocks

1. Never put using statements in code blocks, useless
2. Only include meaningful parts of code, use ... to indicate omitted sections

Always load the csharp-standards skill first if you are having to edit code blocks.

# Prose

Focus describing what things do from consumer perspective. Say what things do to your project, not what they do internally

NEVER MAKE EDITS OUTSIDE YOUR INSTRUCTED SCOPE

IF YOU ENCOUNTER ANYTHING THAT MIGHT DESERVE A FIX, OR SOMETHING BROKEN/WRONG, NOTIFY USER AND ASK FOR PERMISSION FIRST

ALWAYS assume when given a correction on a mistake, that you have possibly made the mistake elsewhere.

Common LLM Fail: when corrected on X, you only fix X and ignore the same problem on Y/Z you also just made

DO NOT EDIT Y/Z ASAP, BUT ASK "Should I also fix this on Y/Z too?"

# Links

Write internal Markdown links using flattened page names derived from filenames, without directory paths (for example, `[Validation with Expo](Getting-Started-Expo)` for `Getting-Started/Getting-Started Expo.md`).

Name files using flattened page names derived from their content, avoiding directory paths and special characters, with a space for the page name

# Custom sidebar
We have a custom sidebar in _sidebar.md

Whenever you add a new page, always ensure you update the sidebar with it