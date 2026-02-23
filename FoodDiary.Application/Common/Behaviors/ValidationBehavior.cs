using FluentValidation;
using FoodDiary.Application.Common.Abstractions.Result;
using MediatR;

namespace FoodDiary.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result {
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        if (!_validators.Any()) {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .Select(f => new Error(
                string.IsNullOrWhiteSpace(f.ErrorCode) ? "Validation.Invalid" : f.ErrorCode,
                f.ErrorMessage))
            .ToArray();

        if (errors.Length == 0) {
            return await next(cancellationToken);
        }

        if (typeof(TResponse) == typeof(Result)) {
            return (TResponse)(object)Result.Failure(errors[0]);
        }

        var resultType = typeof(TResponse);
        if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(Result<>)) throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.");
        var valueType = resultType.GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(Error)]);

        if (failureMethod is null) throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.");
        var genericFailureMethod = failureMethod.MakeGenericMethod(valueType);
        return (TResponse)genericFailureMethod.Invoke(null, [errors[0]])!;
    }
}
