using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Common.Behaviors;

internal sealed class CommandTransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue postCommitActionQueue)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse> {
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        TResponse response = await next(cancellationToken).ConfigureAwait(false);

        if (response is Result { IsFailure: true }) {
            return response;
        }

        if (unitOfWork.HasPendingChanges) {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (postCommitActionQueue.HasActions) {
            await postCommitActionQueue.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
