namespace Brigade.Net.Benchmarks.Api.ParityTests;

[CollectionDefinition("API parity", DisableParallelization = true)]
public sealed class ApiParityCollection : ICollectionFixture<ApiFixture>;
