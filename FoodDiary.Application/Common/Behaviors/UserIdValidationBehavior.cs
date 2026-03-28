using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using MediatR;

namespace FoodDiary.Application.Common.Behaviors;

public class UserIdValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IUserRequest
    where TResponse : Result {

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        if (request.UserId is null || request.UserId.Value == Guid.Empty) {
            var error = Errors.Authentication.InvalidToken;

            if (typeof(TResponse) == typeof(Result)) {
                return (TResponse)(object)Result.Failure(error);
            }

            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>)) {
                var valueType = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])
                    ?.MakeGenericMethod(valueType);

                if (failureMethod is not null) {
                    return (TResponse)failureMethod.Invoke(null, [error])!;
                }
            }

            throw new InvalidOperationException($"Unable to create failure result for type {typeof(TResponse).Name}.");
        }

        return await next(cancellationToken);
    }
}
