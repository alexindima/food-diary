using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Results;
using FoodDiary.Mediator;
using MailInboxResult = FoodDiary.Results.Result;

namespace FoodDiary.MailInbox.Application.Common.Behaviors;

public sealed class MailInboxValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : MailInboxResult {
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        IValidator<TRequest>[] validatorList = [.. validators];
        if (validatorList.Length == 0) {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        ValidationResult[] validationResults = await Task.WhenAll(
            validatorList.Select(validator => validator.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
        ValidationFailure[] errors = [.. validationResults
            .Where(static result => !result.IsValid)
            .SelectMany(static result => result.Errors)];

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
        var error = new Error(
            errorCode,
            errorMessage,
            Kind: ErrorKind.Validation,
            Details: details.Count > 0 ? details : null);

        if (typeof(TResponse) == typeof(MailInboxResult)) {
            return (TResponse)MailInboxResult.Failure(error);
        }

        return CreateTypedFailure(error);
    }

    private static TResponse CreateTypedFailure(Error error) {
        Type resultType = typeof(TResponse);
        if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(Result<>)) {
            throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.");
        }

        Type valueType = resultType.GetGenericArguments()[0];
        MethodInfo? failureMethod = typeof(MailInboxResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(method =>
                string.Equals(method.Name, nameof(MailInboxResult.Failure), StringComparison.Ordinal) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters() is [{ ParameterType: Type parameterType }] &&
                parameterType == typeof(Error))
            ?.MakeGenericMethod(valueType);

        return failureMethod is null
            ? throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.")
            : (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
