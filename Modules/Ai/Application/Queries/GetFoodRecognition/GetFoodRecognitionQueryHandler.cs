using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Queries.GetFoodRecognition;

public sealed class GetFoodRecognitionQueryHandler(IFoodRecognitionJobStore store)
    : IQueryHandler<GetFoodRecognitionQuery, Result<FoodRecognitionJobModel>> {
    public async Task<Result<FoodRecognitionJobModel>> Handle(GetFoodRecognitionQuery request, CancellationToken cancellationToken) {
        FoodRecognitionJobModel? job = await store.GetAsync(request.UserId, request.Id, cancellationToken).ConfigureAwait(false);
        return job is null
            ? Result.Failure<FoodRecognitionJobModel>(AiErrors.RecognitionNotFound())
            : Result.Success(job);
    }
}
