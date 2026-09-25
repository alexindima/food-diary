using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.DeleteFoodRecognition;

public sealed class DeleteFoodRecognitionCommandHandler(IFoodRecognitionJobStore store)
    : IRequestHandler<DeleteFoodRecognitionCommand, Result> {
    public Task<Result> Handle(DeleteFoodRecognitionCommand request, CancellationToken cancellationToken) =>
        store.DeleteCompletedAsync(request.UserId, request.Id, cancellationToken);
}
