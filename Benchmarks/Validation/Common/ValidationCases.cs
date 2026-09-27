namespace Brigade.Net.Benchmarks.Validation.Common;

public static class ValidationCases
{
    private static ValidationData ValidData => new(
        "Alice", "Smith", "alice_123", "alice@example.com", "Toronto",
        "123 Maple Street", "12345", "CA", 30, 75, 2, 5000
    );

    public static ValidationCase Create(string scenario)
    {
        var valid = ValidData;
        var data = scenario switch
        {
            "AllValid" => valid,
            "SomeValid" => valid with
            {
                Username = "x",
                Email = "bad",
                Age = 17,
                Score = 101
            },
            "NonValid" => new ValidationData(
                "A", "B", "x", "bad", "X", "X", "12", "USA", 17, 101, -1, -1
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        return FromData(data);
    }

    public static ValidationCase WithInvalidField(string field)
    {
        var valid = ValidData;
        var data = field switch
        {
            nameof(ValidationData.FirstName) => valid with { FirstName = "A" },
            nameof(ValidationData.LastName) => valid with { LastName = "B" },
            nameof(ValidationData.Username) => valid with { Username = "x" },
            nameof(ValidationData.Email) => valid with { Email = "bad" },
            nameof(ValidationData.City) => valid with { City = "X" },
            nameof(ValidationData.Street) => valid with { Street = "X" },
            nameof(ValidationData.PostalCode) => valid with { PostalCode = "12" },
            nameof(ValidationData.CountryCode) => valid with { CountryCode = "USA" },
            nameof(ValidationData.Age) => valid with { Age = 17 },
            nameof(ValidationData.Score) => valid with { Score = 101 },
            nameof(ValidationData.OrderCount) => valid with { OrderCount = -1 },
            nameof(ValidationData.CreditLimit) => valid with { CreditLimit = -1 },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        return FromData(data);
    }

    public static ValidationCase WithAboveMaximum(string field)
    {
        var valid = ValidData;
        var data = field switch
        {
            nameof(ValidationData.FirstName) => valid with { FirstName = new string('A', 41) },
            nameof(ValidationData.LastName) => valid with { LastName = new string('B', 41) },
            nameof(ValidationData.Username) => valid with { Username = new string('x', 21) },
            nameof(ValidationData.City) => valid with { City = new string('C', 61) },
            nameof(ValidationData.Street) => valid with { Street = new string('S', 101) },
            nameof(ValidationData.PostalCode) => valid with { PostalCode = "123456" },
            nameof(ValidationData.CountryCode) => valid with { CountryCode = "USA" },
            nameof(ValidationData.Age) => valid with { Age = 121 },
            nameof(ValidationData.Score) => valid with { Score = 101 },
            nameof(ValidationData.OrderCount) => valid with { OrderCount = 10001 },
            nameof(ValidationData.CreditLimit) => valid with { CreditLimit = 100001 },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        return FromData(data);
    }

    public static ValidationCase WithMissingField(string field)
    {
        var valid = ValidData;
        var data = field switch
        {
            nameof(ValidationData.FirstName) => valid with { FirstName = null! },
            nameof(ValidationData.LastName) => valid with { LastName = null! },
            nameof(ValidationData.Username) => valid with { Username = null! },
            nameof(ValidationData.Email) => valid with { Email = null! },
            nameof(ValidationData.City) => valid with { City = null! },
            nameof(ValidationData.Street) => valid with { Street = null! },
            nameof(ValidationData.PostalCode) => valid with { PostalCode = null! },
            nameof(ValidationData.CountryCode) => valid with { CountryCode = null! },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        return FromData(data);
    }

    private static ValidationCase FromData(ValidationData data)
    {
        return new ValidationCase(
            ExpoValidationModel.FromData(data),
            ValidlyValidationModel.FromData(data),
            AnnotationValidationModel.FromData(data)
        );
    }
}
