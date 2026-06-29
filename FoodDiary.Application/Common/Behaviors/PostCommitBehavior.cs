using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Common.Behaviors;

internal sealed class PostCommitBehavior<TRequest, TResponse>(IPostCommitActionQueue postCommitActionQueue)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse> {
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        TResponse response = await next(cancellationToken).ConfigureAwait(false);

        if (postCommitActionQueue.HasActions) {
            await postCommitActionQueue.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
