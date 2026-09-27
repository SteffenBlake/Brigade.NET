using FluentValidation;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class CreateItemValidator<TContext> : AbstractValidator<CreateItemCommand<TContext>>
    where TContext : BenchmarkDbContext
{
    public CreateItemValidator()
    {
        RuleFor(command => command.Payload.Title)
            .NotNull().WithMessage("Title is required.")
            .MinimumLength(3).WithMessage("Title is shorter than the minimum length.")
            .MaximumLength(80).WithMessage("Title exceeds the maximum length.");
        RuleFor(command => command.Payload.CategoryId)
            .GreaterThanOrEqualTo(1)
            .WithMessage("CategoryId must be greater than or equal to the configured value.")
            .LessThanOrEqualTo(10)
            .WithMessage("CategoryId must be less than or equal to the configured value.");
        RuleFor(command => command.Payload.Score)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Score must be greater than or equal to the configured value.")
            .LessThanOrEqualTo(100)
            .WithMessage("Score must be less than or equal to the configured value.");
    }
}
