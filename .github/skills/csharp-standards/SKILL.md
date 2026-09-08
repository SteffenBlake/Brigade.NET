---
name: csharp-standards
description: "MANDATORY ALWAYS LOAD. C# code style rules for this repo. Use for ANY C# code write/edit."
---

# C# Standards ⚠️ ALWAYS LOAD

1. Braces `{ }` ALWAYS. NEVER 1-liner if/for/while/etc.
   ```csharp
   // BAD
   if (x) return y;

   // GOOD
   if (x)
   {
       return y;
   }
   ```

2. Multi-line params/args → closing `)` OWN LINE. Never end-of-line.
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

3. Class/record ctor → ALWAYS primary constructor. Never old-style ctor body just to assign field.
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
