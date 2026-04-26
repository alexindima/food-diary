using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Common.Behaviors;

internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse> {
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        var response = await next(cancellationToken);
        if (unitOfWork.HasPendingChanges) {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
