using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Contracts.Queries.GetCompletedFoodRecognition;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Application.Queries.GetCompletedFoodRecognition;

public sealed class GetCompletedFoodRecognitionQueryHandler(IFoodRecognitionJobReader jobs) : IRequestHandler<GetCompletedFoodRecognitionQuery, Result<FoodRecognitionJobModel>> {
    public async Task<Result<FoodRecognitionJobModel>> Handle(GetCompletedFoodRecognitionQuery request, CancellationToken cancellationToken) {
        Guid ownerId = request.UserId;
        Guid recognitionId = request.JobId;
        var userId = new UserId(ownerId);
        var jobId = new FoodRecognitionJobId(recognitionId);
        FoodRecognitionJobModel? job = await jobs.GetAsync(userId, jobId, cancellationToken).ConfigureAwait(false);
        if (job is null || job.UserId != userId.Value || job.Id != jobId.Value) {
            return Result.Failure<FoodRecognitionJobModel>(new Error("Ai.RecognitionNotFound", "Recognition result was not found.", ErrorKind.NotFound));
        }
        return FoodRecognitionResult.Decode(job) switch {
            FoodRecognitionResult.Pending => Result.Failure<FoodRecognitionJobModel>(new Error("Ai.RecognitionNotReady", "Recognition has not completed.", ErrorKind.Conflict)),
            FoodRecognitionResult.Ready ready => Result.Success(ready.Job),
            _ => Result.Failure<FoodRecognitionJobModel>(new Error("Ai.RecognitionNotUsable", "Recognition does not contain a complete nutrition result.", ErrorKind.Validation)),
        };
    }

}
