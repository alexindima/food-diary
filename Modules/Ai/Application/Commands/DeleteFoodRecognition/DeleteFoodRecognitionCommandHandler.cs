using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.DeleteFoodRecognition;

public sealed class DeleteFoodRecognitionCommandHandler(IFoodRecognitionJobStore store)
    : IRequestHandler<DeleteFoodRecognitionCommand, Result> {
    public Task<Result> Handle(DeleteFoodRecognitionCommand request, CancellationToken cancellationToken) {
        Guid ownerId = request.UserId;
        Guid recognitionId = request.Id;
        return store.DeleteCompletedAsync(new UserId(ownerId), new FoodRecognitionJobId(recognitionId), cancellationToken);
    }
}
