using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
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
            .ToArray();

        if (errors.Length == 0) {
            return await next(cancellationToken);
        }

        var groupedDetails = errors
            .Where(static error => !string.IsNullOrWhiteSpace(error.PropertyName))
            .GroupBy(
                static error => error.PropertyName,
                StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(static error => error.ErrorMessage)
                    .Where(static message => !string.IsNullOrWhiteSpace(message))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        var firstError = errors[0];
        var errorCode = string.IsNullOrWhiteSpace(firstError.ErrorCode) ? "Validation.Invalid" : firstError.ErrorCode;
        var errorMessage = groupedDetails.Count > 1 || groupedDetails.Values.Any(static messages => messages.Length > 1)
            ? "One or more validation errors occurred."
            : firstError.ErrorMessage;
        var error = new Error(
            errorCode,
            errorMessage,
            groupedDetails.Count > 0 ? groupedDetails : null,
            ErrorKindResolver.Resolve(errorCode));

        if (typeof(TResponse) == typeof(Result)) {
            return (TResponse)(object)Result.Failure(error);
        }

        var resultType = typeof(TResponse);
        if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(Result<>)) throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.");
        var valueType = resultType.GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(Error)]);

        if (failureMethod is null) throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.");
        var genericFailureMethod = failureMethod.MakeGenericMethod(valueType);
        return (TResponse)genericFailureMethod.Invoke(null, [error])!;
    }
}
