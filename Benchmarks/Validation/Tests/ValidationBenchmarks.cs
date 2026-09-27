using BenchmarkDotNet.Attributes;
using Brigade.Net.Benchmarks.Validation.Common;

namespace Brigade.Net.Benchmarks.Validation.Tests;

[MemoryDiagnoser]
public class ValidationBenchmarks
{
    private ValidationCase validationCase = null!;

    [Params("AllValid", "SomeValid", "NonValid")]
    public string Scenario { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        validationCase = ValidationCases.Create(Scenario);
    }

    [Benchmark(Baseline = true)]
    public int Expo()
    {
        return ValidationWorkload.Expo(validationCase.Expo);
    }

    [Benchmark]
    public int Validly()
    {
        return ValidationWorkload.Validly(validationCase.Validly);
    }

    [Benchmark]
    public int FluentValidation()
    {
        return ValidationWorkload.FluentValidation(validationCase.Standard);
    }

    [Benchmark]
    public int DataAnnotations()
    {
        return ValidationWorkload.DataAnnotations(validationCase.Standard);
    }
}
