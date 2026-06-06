using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result {
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        if (!_validators.Any()) {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

        ValidationFailure[] errors = [.. validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)];

        if (errors.Length == 0) {
            return await next(cancellationToken).ConfigureAwait(false);
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

        ValidationFailure firstError = errors[0];
        string errorCode = string.IsNullOrWhiteSpace(firstError.ErrorCode) ? "Validation.Invalid" : firstError.ErrorCode;
        string errorMessage = groupedDetails.Count > 1 || groupedDetails.Values.Any(static messages => messages.Length > 1)
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

        Type resultType = typeof(TResponse);
        if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(Result<>)) {
            throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.");
        }

        Type valueType = resultType.GetGenericArguments()[0];
        MethodInfo? failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(Error)]);

        MethodInfo genericFailureMethod = failureMethod!.MakeGenericMethod(valueType);
        return (TResponse)genericFailureMethod.Invoke(null, [error])!;
    }
}
