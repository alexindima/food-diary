using System.Security.Cryptography;
using System.Text;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Services;

public sealed class FoodRecognitionProcessor(IFoodRecognitionJobStore store, ISender sender) : IFoodRecognitionProcessor {
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken) {
        await store.MaintainAsync(cancellationToken).ConfigureAwait(false);
        FoodRecognitionJobModel? job = await store.ClaimAsync(cancellationToken).ConfigureAwait(false);
        if (job is null) {
            return false;
        }

        // A claimed operation is never automatically dispatched again: a lost provider response
        // cannot prove that the paid request was not processed. Maintenance marks it interrupted.
        Result<FoodVisionModel> vision = await sender.Send(new AnalyzeFoodImageCommand(
            job.UserId, job.ImageAssetId, job.Description, RequestId(job.Id, "vision")), cancellationToken).ConfigureAwait(false);
        if (vision.IsFailure) {
            await store.CompleteAsync(job.Id, nutrition: null, vision.Error.Code, nutritionErrorCode: null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        if (!await store.SaveVisionAsync(job.Id, vision.Value, cancellationToken).ConfigureAwait(false)) {
            return true;
        }
        if (vision.Value.Items.Count == 0) {
            await store.CompleteAsync(job.Id, nutrition: null, errorCode: null, nutritionErrorCode: null, cancellationToken).ConfigureAwait(false);
            return true;
        }

        Result<FoodNutritionModel> nutrition = await sender.Send(new CalculateFoodNutritionCommand(
            job.UserId, vision.Value.Items, RequestId(job.Id, "nutrition")), cancellationToken).ConfigureAwait(false);
        await store.CompleteAsync(
            job.Id, nutrition.IsSuccess ? nutrition.Value : null, errorCode: null,
            nutrition.IsFailure ? nutrition.Error.Code : null, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static string RequestId(Guid id, string operation) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"food-recognition:{id:D}:{operation}")));
}
