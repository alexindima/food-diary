using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Contracts.Queries.GetCompletedFoodRecognition;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Application.Queries.GetCompletedFoodRecognition;

public sealed class GetCompletedFoodRecognitionQueryHandler(IFoodRecognitionJobReader jobs) : IRequestHandler<GetCompletedFoodRecognitionQuery, Result<FoodRecognitionJobModel>> {
    public async Task<Result<FoodRecognitionJobModel>> Handle(GetCompletedFoodRecognitionQuery request, CancellationToken cancellationToken) {
        Guid userId = request.UserId;
        Guid jobId = request.JobId;
        FoodRecognitionJobModel? job = await jobs.GetAsync(userId, jobId, cancellationToken).ConfigureAwait(false);
        if (job is null || job.UserId != userId || job.Id != jobId) {
            return Result.Failure<FoodRecognitionJobModel>(new Error("Ai.RecognitionNotFound", "Recognition result was not found.", ErrorKind.NotFound));
        }
        if (string.Equals(job.Status, "Queued", StringComparison.Ordinal) || string.Equals(job.Status, "Running", StringComparison.Ordinal)) {
            return Result.Failure<FoodRecognitionJobModel>(new Error("Ai.RecognitionNotReady", "Recognition has not completed.", ErrorKind.Conflict));
        }
        if (!string.Equals(job.Status, "Succeeded", StringComparison.Ordinal) || job.ErrorCode is not null ||
            job.NutritionErrorCode is not null || job.Nutrition?.Items is not { Count: > 0 } || job.Vision?.Items is not { Count: > 0 } ||
            job.Nutrition.Items.Count != job.Vision.Items.Count) {
            return Result.Failure<FoodRecognitionJobModel>(new Error("Ai.RecognitionNotUsable", "Recognition does not contain a complete nutrition result.", ErrorKind.Validation));
        }
        return Result.Success(job);

    }

}
