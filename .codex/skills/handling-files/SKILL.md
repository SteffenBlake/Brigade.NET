---
name: handling-files
description: Read, make, remove, append, patch local text files small command, guarded edits. Save tokens.
---

# HANDLE FILE 🪨

USE FEW TOKEN. CHANGE ONLY NEED.

## READ

READ ONLY LINE RANGE NEEDED. SHOW LINE NUMBER:

```sh
nl -ba FILE | sed -n 'X,Yp'
```

USE `rg -n 'TEXT' FILE` TO FIND TEXT FIRST.

## MAKE

USE ONE `apply_patch` ADD-FILE PATCH. MAKE WHOLE FILE ONCE. NO MANY SHELL CALL.

## REMOVE

USE `rm FILE`. CHECK PATH FIRST WHEN PATH MAY BE WRONG. NO BIG WILD CARD.

## APPEND

USE APPEND REDIRECT. DO NOT READ + WRITE WHOLE FILE:

```sh
printf '%s\n' 'NEW TEXT' >> FILE
```

FOR MANY LINE, USE `cat >> FILE <<'EOF'`.

## PATCH

USE BUILT-IN `apply_patch` TOOL. 
