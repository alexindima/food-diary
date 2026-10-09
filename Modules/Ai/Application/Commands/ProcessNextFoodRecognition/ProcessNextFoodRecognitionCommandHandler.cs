using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Contracts.Commands.ProcessNextFoodRecognition;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Commands.AnalyzeFoodImage;
using FoodDiary.Modules.Ai.Application.Commands.CalculateFoodNutrition;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.ProcessNextFoodRecognition;

public sealed class ProcessNextFoodRecognitionCommandHandler(IFoodRecognitionJobStore store, ISender sender)
    : IRequestHandler<ProcessNextFoodRecognitionCommand, bool> {
    public async Task<bool> Handle(ProcessNextFoodRecognitionCommand request, CancellationToken cancellationToken) {
        await store.MaintainAsync(cancellationToken).ConfigureAwait(false);
        FoodRecognitionJobModel? job = await store.ClaimAsync(cancellationToken).ConfigureAwait(false);
        if (job is null) {
            return false;
        }

        var jobId = new FoodRecognitionJobId(job.Id);

        // A claimed operation is never automatically dispatched again: a lost provider response
        // cannot prove that the paid request was not processed. Maintenance marks it interrupted.
        Result<FoodVisionModel> vision = await sender.Send(new AnalyzeFoodImageCommand(
            job.UserId, job.ImageAssetId, job.Description, RequestId(jobId, "vision"), job.IsProductLabel, (job.AdditionalImages ?? []).Select(x => x.ImageAssetId).ToArray()), cancellationToken).ConfigureAwait(false);
        if (vision.IsFailure) {
            await store.CompleteAsync(jobId, nutrition: null, vision.Error.Code, nutritionErrorCode: null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        if (!await store.SaveVisionAsync(jobId, vision.Value, cancellationToken).ConfigureAwait(false)) {
            return true;
        }
        if (job.IsProductLabel || vision.Value.Items.Count == 0) {
            await store.CompleteAsync(jobId, nutrition: null, errorCode: null, nutritionErrorCode: null, cancellationToken).ConfigureAwait(false);
            return true;
        }

        Result<FoodNutritionModel> nutrition = await sender.Send(new CalculateFoodNutritionCommand(
            job.UserId, vision.Value.Items, RequestId(jobId, "nutrition")), cancellationToken).ConfigureAwait(false);
        await store.CompleteAsync(
            jobId, nutrition.IsSuccess ? nutrition.Value : null, errorCode: null,
            nutrition.IsFailure ? nutrition.Error.Code : null, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static string RequestId(FoodRecognitionJobId id, string operation) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"food-recognition:{id.Value:D}:{operation}")));
}
