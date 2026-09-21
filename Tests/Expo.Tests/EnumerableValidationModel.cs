namespace Brigade.Net.Expo.Tests;

[Expo]
public partial class EnumerableValidationModel
{
    public GeneratedChildModel[]? ArrayChildren { get; init; }

    public List<GeneratedChildModel>? ListChildren { get; init; }

    public HashSet<GeneratedChildModel>? HashSetChildren { get; init; }

    public IEnumerable<GeneratedChildModel>? EnumerableChildren { get; init; }
}
