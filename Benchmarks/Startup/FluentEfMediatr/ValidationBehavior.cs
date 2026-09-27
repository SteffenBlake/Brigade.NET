using FluentValidation;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct
    )
    {
        foreach (var validator in validators)
        {
            await validator.ValidateAndThrowAsync(request, ct);
        }

        return await next();
    }
}
