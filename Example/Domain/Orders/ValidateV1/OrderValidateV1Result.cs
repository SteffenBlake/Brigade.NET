namespace Brigade.Net.Example.Domain.Orders.ValidateV1;

public sealed record OrderValidateV1Result(
    bool Accepted,
    string CustomCode
);
