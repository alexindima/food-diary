using FluentValidation;
using FoodDiary.Application.Common.Abstractions.Result;
using MediatR;

namespace FoodDiary.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior для автоматической валидации команд и запросов
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = failures
                .Select(f => new Error(f.ErrorCode, f.ErrorMessage))
                .ToArray();

            // Создаем Result.Failure<TValue> с первой ошибкой
            // Извлекаем TValue из Result<TValue>
            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), 1, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new[] { typeof(Error) }, null);

                if (failureMethod != null)
                {
                    var genericFailureMethod = failureMethod.MakeGenericMethod(valueType);
                    return (TResponse)genericFailureMethod.Invoke(null, new object[] { errors[0] })!;
                }
            }

            throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}");
        }

        return await next();
    }
}
