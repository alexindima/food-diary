using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Services;

public sealed class FoodRecognitionResultReader(IFoodRecognitionJobStore jobs) : IFoodRecognitionResultReader {
    public async Task<Result<FoodRecognitionJobModel>> GetCompletedAsync(Guid userId, Guid jobId, CancellationToken cancellationToken) {
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
