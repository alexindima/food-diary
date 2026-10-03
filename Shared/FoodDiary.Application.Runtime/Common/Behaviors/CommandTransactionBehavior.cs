using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Mediator;
using FoodDiary.Application.Runtime.Common.Services;

namespace FoodDiary.Application.Runtime.Common.Behaviors;

internal sealed class CommandTransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue postCommitActionQueue,
    IAtomicCommandExecutor? atomicCommandExecutor = null,
    CommandExecutionScope? executionScope = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalCommand {
    private readonly CommandExecutionScope _executionScope = executionScope ?? new CommandExecutionScope();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        using CommandExecutionScope.Lease scope = _executionScope.Enter();
        if (!scope.IsOutermost) {
            if (request is IAtomicCommand) {
                _executionScope.MarkNestedFailure();
                throw new InvalidOperationException("An atomic command must own the outer command boundary.");
            }
            try {
                TResponse nestedResponse = await next(cancellationToken).ConfigureAwait(false);
                if (nestedResponse is Result { IsFailure: true }) {
                    _executionScope.MarkNestedFailure();
                }
                return nestedResponse;
            } catch {
                _executionScope.MarkNestedFailure();
                throw;
            }
        }
        try {
            bool atomic = request is IAtomicCommand;
            TResponse response;
            if (atomic) {
                IAtomicCommandExecutor executor = atomicCommandExecutor ?? throw new InvalidOperationException("Atomic commands require a persistence executor.");
                if (request is IIdempotentAtomicCommand { Operation: { } operation } && request is IUserRequest userRequest
                    && operation.UserId != userRequest.UserId) {
                    throw new InvalidOperationException("The atomic operation must belong to the command's authenticated user.");
                }
                response = request is IIdempotentAtomicCommand { Operation: { } identity }
                    ? await executor.ExecuteAsync(identity, ExecuteHandlerAsync, cancellationToken).ConfigureAwait(false)
                    : await executor.ExecuteAsync(ExecuteHandlerAsync, cancellationToken).ConfigureAwait(false);
            } else {
                response = await ExecuteHandlerAsync(cancellationToken).ConfigureAwait(false);
            }

            if (response is Result { IsFailure: true }) {
                unitOfWork.DiscardChanges();
                postCommitActionQueue.Discard();
                return response;
            }

            if (!atomic && unitOfWork.HasPendingChanges) {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            if (postCommitActionQueue.HasActions) {
                await postCommitActionQueue.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }

            return response;
        } catch {
            unitOfWork.DiscardChanges();
            postCommitActionQueue.Discard();
            throw;
        }

        async Task<TResponse> ExecuteHandlerAsync(CancellationToken token) {
            _executionScope.ResetNestedFailures();
            TResponse result = await next(token).ConfigureAwait(false);
            if (_executionScope.HasFailedNestedCommand && result is not Result { IsFailure: true }) {
                throw new InvalidOperationException("A failed nested command prevents committing the outer command.");
            }
            return result;
        }
    }
}
