using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Queries.GetFoodRecognition;

public sealed class GetFoodRecognitionQueryHandler(IFoodRecognitionJobReader store)
    : IQueryHandler<GetFoodRecognitionQuery, Result<FoodRecognitionJobModel>> {
    public async Task<Result<FoodRecognitionJobModel>> Handle(GetFoodRecognitionQuery request, CancellationToken cancellationToken) {
        FoodRecognitionJobModel? job = await store.GetAsync(request.UserId, request.Id, cancellationToken).ConfigureAwait(false);
        return job is null
            ? Result.Failure<FoodRecognitionJobModel>(AiErrors.RecognitionNotFound())
            : Result.Success(job);
    }
}
