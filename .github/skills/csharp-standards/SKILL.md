---
name: csharp-standards
description: "MUST LOAD. All C# edits. One class per file. RoutePolicy suffix."
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

3. Class/record ctor: primary ONLY. No old ctor for field set.
   ```csharp
   // BAD
   public class Foo
   {
       private readonly int _x;

       public Foo(int x)
       {
           _x = x;
       }
   }

   // GOOD
   public class Foo(int x)
   {
       private readonly int _x = x;
   }
   ```

4. ONE class per file. No class pile. File name match class name.
   Exception: a handler/provider/Partie's domain context record goes in its owner's file, directly after the namespace and before the owner type.
   Exception: child DTOs belong in their owning command/query/result file. Keep DTOs specific to each operation, even when their fields match.

5. Route policy class name MUST end `RoutePolicy`.

   CQRS names: `<DomainSlice><Search|Create|Update|Delete><Version><Cmd|Query|Result|Handler>`.
   Group each operation under its versioned folder, e.g. `Orders/CreateV1`. Shared domain types stay in `Orders`.
   Commands without response data return `Unit`; creation can return an ID. One Search operation supports ID filters; no separate Get-by-ID operation.

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
