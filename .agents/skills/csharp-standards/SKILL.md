---
name: csharp-standards
description: "MUST LOAD. All C# edits. One class per file. RoutePolicy suffix."
metadata:
  internal: true
---

# C# Rules. MUST LOAD.

1. Braces ALWAYS. No bare if/for/while.
   ```csharp
   // BAD
   if (x) return y;
   if (x) { return y; }

   // GOOD
   if (x)
   {
       return y;
   }
   ```

2. Args span lines? Close `)` on own line.
   ```csharp
   // BAD
   public void Foo(
       int a,
       int b) { }

   // GOOD
   public void Foo(
       int a,
       int b)
   {
   }
   ```

3. Use primary ctor for class and record with inputs. Handler, provider, service too. Use ctor args as-is when you can. Make field only if needed. Old ctor just to set fields? NO. Old ctor okay only for setup or checks that primary ctor cannot show clear.
   ```csharp
   // BAD
   public class Foo
   {
       private readonly int _x;

       public Foo(int x)
       {
           _x = x;
       }

       public int GetValue() => _x;
   }

   // GOOD
   public class Foo(int x)
   {
       public int GetValue() => x;
   }
   ```

4. ONE class per file. No class pile. File name match class name.
   Exception: a handler/provider/Partie's domain context record goes in its owner's file, directly after the namespace and before the owner type.

   Exception: child DTOs belong in their owning command/query/result file. Keep DTOs specific to each operation, even when their fields match.

   Exception: Small "owned" record DTOs that "belong" to core of main logic, declare at top, especially if record only is used in that file

   Not Exception: Queries, Commands, Results, anything part of types public api that outsiders need

6. Func sigs with 3+ params multilined

void SomeFunc(
    int a,
    string b,
    bool c
)

7. Func calls with args > 100 columns multilined

var result = aFunc(
    aLongVariableName,
    anotherLongerVariableName,
    yetAnotherLongVariableName
);

8. Keep nesting at three levels or less. Prefer guard clauses, invert conditions so the short branch is nested, and extract the outer operation when a block still grows. A fourth level is acceptable only for one short line.

9. One blank line between chunks in a method. Keep small setup vars as one chunk. Each big build or calc gets own chunk: `anchor`, blank line, `recursive`, blank line, `categories`, blank line, `query`. Gap before side effect or final return too. New step after a long chain? Add gap. No gap inside one fluent chain. No gap after each short, linked line. Blank line between members. Each generic constraint gets own line.

10. Use expression bodies only when the whole declaration fits clearly on one short line. Use a block body for multiline declarations.

11. Put primary-constructor parameters on separate lines when the declaration is long.

12. Prefer a record for an immutable data carrier with value semantics. Keep a class when identity, mutable state, inheritance, or custom equality matters.
