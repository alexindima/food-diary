using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Mediator;
using MailRelayResult = FoodDiary.MailRelay.Application.Common.Results.Result;

namespace FoodDiary.MailRelay.Application.Common.Behaviors;

public sealed class MailRelayValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : MailRelayResult {
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        IValidator<TRequest>[] validatorList = validators.ToArray();
        if (validatorList.Length == 0) {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        ValidationResult[] validationResults = await Task.WhenAll(
            validatorList.Select(validator => validator.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
        ValidationFailure[] errors = validationResults
            .Where(static result => !result.IsValid)
            .SelectMany(static result => result.Errors)
            .ToArray();

        if (errors.Length == 0) {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var details = errors
            .Where(static error => !string.IsNullOrWhiteSpace(error.PropertyName))
            .GroupBy(static error => error.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(static error => error.ErrorMessage)
                    .Where(static message => !string.IsNullOrWhiteSpace(message))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        ValidationFailure firstError = errors[0];
        string errorCode = string.IsNullOrWhiteSpace(firstError.ErrorCode)
            ? "Validation.Invalid"
            : firstError.ErrorCode;
        string errorMessage = details.Count > 1 || details.Values.Any(static messages => messages.Length > 1)
            ? "One or more validation errors occurred."
            : firstError.ErrorMessage;
        var error = new MailRelayError(
            errorCode,
            errorMessage,
            ErrorKind.Validation,
            details.Count > 0 ? details : null);

        if (typeof(TResponse) == typeof(MailRelayResult)) {
            return (TResponse)(object)MailRelayResult.Failure(error);
        }

        Type resultType = typeof(TResponse);
        if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(Result<>)) {
            throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.");
        }

        Type valueType = resultType.GetGenericArguments()[0];
        MethodInfo? failureMethod = typeof(Result<>)
            .MakeGenericType(valueType)
            .GetMethod(nameof(Result<object>.Failure), [typeof(MailRelayError)]);

        return failureMethod is null
            ? throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.")
            : (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
