---
name: brigade-net-expo
description: Use Brigade.Net.Expo in application models: built-in and generated validation attributes, custom rules, nested validation, errors, and generated metadata. Assumes Expo and its generators are already installed; do not use for package or host setup.
---

## Model contract

Mark with `[Expo]` `partial`

[Expo]
public partial class CreateOrder

`[Expo]` adds `IExpoValidatable`, `TryValidate(out IEnumerable<ErrorDetail> errors)`, static `ExpoModelMetadata Metadata`

Generator handles declared instance properties except indexers. Static properties and indexers ignored. validates every rule aggregates all failures rather than stopping at the first one. Each `ErrorDetail.Pointer` is an RFC 6901  path like `/Customer` or `/Lines/0/Sku`.

Null is valid for every built-in format, length, empty, whitespace, and nullable-enum rule. Add `[IsRequired]` when null must fail. `[IsNotEmpty]` rejects non-null zero-length value; `[IsNotWhiteSpace]` rejects non-null null/empty/whitespace string.

## Static attributes

Rules take optional last `message`.

- null: `[IsRequired]`
- constants: `[IsGreaterThan(value)]`, `[IsGreaterThanOrEqualTo(value)]`, `[IsLessThan(value)]`, `[IsLessThanOrEqualTo(value)]`, `[IsEqualTo(value)]`, `[IsNotEqualTo(value)]`
- size: `[HasMinimumLength(n)]`, `[HasMaximumLength(n)]`, `[HasExactLength(n)]`, `[IsNotEmpty]`
- string: `[IsNotWhiteSpace]`, `[IsEmail]`, `[IsPhoneNumber]`, `[IsUuid]`, `[IsUrl]`, `[IsIpAddress]`, `[IsIpv4Address]`, `[IsIpv6Address]`, `[IsBase64]`, `[IsHexColor]`, `[IsSlug]`, `[IsAlpha]`, `[IsAlphaNumeric]`, `[IsDigits]`
- enum: `[IsDefinedEnum]`
- gen markers: `[IsComparable]`, `[CustomValidation]`

Compare uses `Comparer<T>.Default`; equal uses `EqualityComparer<T>.Default`. Constant must convert to prop type. Size uses `Length`, `Count`, else enumerates `IEnumerable<T>`. `[IsNotWhiteSpace]` string only.

Do not use abstract `ValueComparisonAttribute`, `PropertyComparisonAttribute`, `LengthValidationAttribute`, or `Is*XAttribute` direct.

## Source-generated attributes

Private to same Expo model.

`[IsComparable]` on `Start` gens `[IsGreaterThanStart]`, `[IsGreaterThanOrEqualToStart]`, `[IsLessThanStart]`, `[IsLessThanOrEqualToStart]`, `[IsEqualToStart]`, `[IsNotEqualToStart]`. Put on compatible prop. Optional message only.

```csharp
[IsComparable]
public int Start { get; init; }
[IsGreaterThanOrEqualToStart]
public int End { get; init; }
```

`[GeneratedRegex]` static no-arg member gens `[Matches<MemberName>]`. Null passes.

```csharp
[GeneratedRegex(@"^REF-\d{4}$")]
private static partial Regex RefRegex();
[MatchesRefRegex]
public string? Ref { get; init; }
```

## Custom validation

Reusable rule: attribute implements `IExpoValidationAttribute`; static `bool IsValid(object? value)`. Rule owns null logic. Failure default `Prop is invalid.` Constructor/named `Message` can override.

```csharp
public sealed class IsEvenAttribute : Attribute, IExpoValidationAttribute
{
    public static bool IsValid(object? value) => value is int n && n % 2 == 0;
}
```

Model rule: `[CustomValidation]` on prop; exact `private partial IEnumerable<string> Validate<Prop>()`. Each string one error at prop pointer. Empty means pass.

```csharp
[CustomValidation]
public string Code { get; init; } = string.Empty;
private partial IEnumerable<string> ValidateCode() =>
    Code.StartsWith("ORD-") ? [] : ["Bad code."];
```

## Nest + output

Non-null `IExpoValidatable` prop cascades. Array/generic `IEnumerable<T>` cascades by index when `T` validatable. Mark child `[Expo] partial`. Null child/list passes unless `[IsRequired]`; null item skipped.

`Metadata.Properties`: name, escaped pointer, ordered rules, nested flag. Rules: kind, constant, compared prop, message, custom name, pattern, format.

Validation only when `TryValidate` called. `ExpoValidationPartie<TRequest,TResult>` calls it; fail returns `Error(errors)`, pass continues.

Must add ExpoValidationPartie to Routing for automatic validation. See brigade-net-partie skill for further details.
