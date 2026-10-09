using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Queries.GetFoodRecognition;

public sealed class GetFoodRecognitionQueryHandler(IFoodRecognitionJobReader store)
    : IQueryHandler<GetFoodRecognitionQuery, Result<FoodRecognitionJobModel>> {
    public async Task<Result<FoodRecognitionJobModel>> Handle(GetFoodRecognitionQuery request, CancellationToken cancellationToken) {
        Guid ownerId = request.UserId;
        Guid recognitionId = request.Id;
        FoodRecognitionJobModel? job = await store.GetAsync(new UserId(ownerId), new FoodRecognitionJobId(recognitionId), cancellationToken).ConfigureAwait(false);
        return job is null
            ? Result.Failure<FoodRecognitionJobModel>(AiErrors.RecognitionNotFound())
            : Result.Success(job);
    }
}
