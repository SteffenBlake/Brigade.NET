namespace Brigade.Net.Expo.Tests;

[Expo]
public partial class GeneratedValidationModel
{
    [IsComparable]
    public int Baseline { get; init; } = 5;

    [IsRequired("Name needed")]
    public string? Name { get; init; }

    [IsGreaterThan(10)]
    public int Greater { get; init; }

    [IsGreaterThanOrEqualTo(10)]
    public int GreaterOrEqual { get; init; }

    [IsLessThan(10)]
    public int Less { get; init; }

    [IsLessThanOrEqualTo(10)]
    public int LessOrEqual { get; init; }

    [IsEqualTo(10)]
    public int Equal { get; init; }

    [IsNotEqualTo(10)]
    public int NotEqual { get; init; }

    [IsGreaterThanBaseline]
    public int AfterBaseline { get; init; }

    [CustomExpo]
    public string? StaticCustom { get; init; }

    [CustomValidation]
    public string PartialCustom { get; init; } = string.Empty;

    public GeneratedChildModel? Child { get; init; }

    private partial IEnumerable<string> ValidatePartialCustom()
    {
        return PartialCustom == "good" ? [] : ["PartialCustom failed."];
    }
}

[Expo]
public partial class GeneratedChildModel
{
    [IsRequired]
    public string? Value { get; init; }

    public GeneratedGrandchildModel? Grandchild { get; init; }
}

[Expo]
public partial class GeneratedGrandchildModel
{
    [IsRequired]
    public string? Value { get; init; }
}
