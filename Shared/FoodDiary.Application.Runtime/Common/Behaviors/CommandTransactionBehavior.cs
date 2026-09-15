using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Runtime.Common.Behaviors;

internal sealed class CommandTransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue postCommitActionQueue,
    IAtomicCommandExecutor? atomicCommandExecutor = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalCommand {
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        bool atomic = request is IAtomicCommand;
        TResponse response = atomic
            ? await (atomicCommandExecutor ?? throw new InvalidOperationException("Atomic commands require a persistence executor."))
                .ExecuteAsync(token => next(token), cancellationToken).ConfigureAwait(false)
            : await next(cancellationToken).ConfigureAwait(false);

        if (response is Result { IsFailure: true }) {
            return response;
        }

        if (!atomic && unitOfWork.HasPendingChanges) {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (postCommitActionQueue.HasActions) {
            await postCommitActionQueue.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return response;
    }
}
