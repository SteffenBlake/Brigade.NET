---
name: csharp-standards
description: "MUST LOAD. All C# edits. One class per file. RoutePolicy suffix."
---

# C# Rules. MUST LOAD.

1. Braces ALWAYS. No bare if/for/while.
   ```csharp
   // BAD
   if (x) return y;

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

5. Route policy class name MUST end `RoutePolicy`.
