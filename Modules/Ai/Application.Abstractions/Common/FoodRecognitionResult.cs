using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Domain.Recognition;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

/// <summary>Decodes the existing job contract into the states a meal consumer can use.</summary>
public abstract class FoodRecognitionResult {
    private FoodRecognitionResult() { }

    public sealed class Pending : FoodRecognitionResult;
    public sealed class Unusable : FoodRecognitionResult;

    public sealed class Ready : FoodRecognitionResult {
        public FoodRecognitionJobModel Job { get; }
        public FoodVisionModel Vision { get; }
        public FoodNutritionModel Nutrition { get; }

        private Ready(FoodRecognitionJobModel job, FoodVisionModel vision, FoodNutritionModel nutrition) {
            Job = job;
            Vision = vision;
            Nutrition = nutrition;
        }

        internal static FoodRecognitionResult DecodeCompleted(FoodRecognitionJobModel job) {
            if (FoodRecognitionLifecycle.Decode(job.Status) != FoodRecognitionStatus.Succeeded || job.ErrorCode is not null ||
                job.NutritionErrorCode is not null || job.Vision is not { Items.Count: > 0 } vision ||
                job.Nutrition is not { Items.Count: > 0 } nutrition || vision.Items.Count != nutrition.Items.Count) {
                return new Unusable();
            }
            return new Ready(job, vision, nutrition);
        }
    }

    public static FoodRecognitionResult Decode(FoodRecognitionJobModel job) {
        ArgumentNullException.ThrowIfNull(job);
        return FoodRecognitionLifecycle.IsPending(FoodRecognitionLifecycle.Decode(job.Status))
            ? new Pending()
            : Ready.DecodeCompleted(job);
    }
}
