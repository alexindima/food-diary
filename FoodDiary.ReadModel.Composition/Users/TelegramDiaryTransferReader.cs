using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Users;

internal sealed class TelegramDiaryTransferReader(ICompositionReadContext context) : ITelegramDiaryTransferReader {
    public async Task<Result<TelegramDiaryCounts>> ReadAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken) {
        if (await HasUnsupportedRecordsAsync(sourceId, cancellationToken)) {
            return Result.Failure<TelegramDiaryCounts>(new Error("User.TelegramDiaryTransferUnavailable",
                "The Telegram diary has records that require a separate transfer.", ErrorKind.Conflict));
        }

        IQueryable<WeightEntry> sourceWeights = context.WeightEntries.AsNoTracking().Where(item => item.UserId == sourceId);
        IQueryable<WaistEntry> sourceWaists = context.WaistEntries.AsNoTracking().Where(item => item.UserId == sourceId);
        bool overlappingMeasurements = await sourceWeights.AnyAsync(item => context.WeightEntries.AsNoTracking()
                .Any(target => target.UserId == targetId && target.Date == item.Date), cancellationToken) ||
            await sourceWaists.AnyAsync(item => context.WaistEntries.AsNoTracking()
                .Any(target => target.UserId == targetId && target.Date == item.Date), cancellationToken);
        if (overlappingMeasurements) {
            return Result.Failure<TelegramDiaryCounts>(new Error("User.TelegramDiaryMeasurementConflict",
                "Both diaries contain a measurement of the same kind for the same day.", ErrorKind.Conflict));
        }

        TelegramDiaryCounts counts = await context.Users.AsNoTracking().Where(user => user.Id == sourceId)
            .Select(_ => new TelegramDiaryCounts(
                context.Meals.AsNoTracking().Count(item => item.UserId == sourceId),
                context.Recipes.AsNoTracking().Count(item => item.UserId == sourceId),
                context.Products.AsNoTracking().Count(item => item.UserId == sourceId),
                context.HydrationEntries.AsNoTracking().Count(item => item.UserId == sourceId),
                sourceWeights.Count() + sourceWaists.Count()))
            .SingleAsync(cancellationToken);
        return Result.Success(counts);
    }

    private Task<bool> HasUnsupportedRecordsAsync(UserId userId, CancellationToken cancellationToken) =>
        context.Users.AsNoTracking().AnyAsync(user => user.Id == userId && (
            context.BillingSubscriptions.AsNoTracking().Any(item => item.UserId == userId) ||
            context.BillingPayments.AsNoTracking().Any(item => item.UserId == userId) ||
            context.BillingCancellations.AsNoTracking().Any(item => item.UserId == userId) ||
            context.PrivacyExportCycleProfile.AsNoTracking().Any(item => item.UserId == userId) ||
            context.PrivacyExportExerciseEntry.AsNoTracking().Any(item => item.UserId == userId) ||
            context.PrivacyExportFastingPlan.AsNoTracking().Any(item => item.UserId == userId) ||
            context.PrivacyExportFastingSession.AsNoTracking().Any(item => item.UserId == userId) ||
            context.PrivacyExportFastingCheckIn.AsNoTracking().Any(item => item.UserId == userId) ||
            context.PrivacyExportWearableConnection.AsNoTracking().Any(item => item.UserId == userId) ||
            context.PrivacyExportWearableSyncEntry.AsNoTracking().Any(item => item.UserId == userId) ||
            context.PrivacyExportWeeklyGoal.AsNoTracking().Any(item => item.UserId == userId) ||
            context.UserLessonProgress.AsNoTracking().Any(item => item.UserId == userId) ||
            context.MealPlans.AsNoTracking().Any(item => item.UserId == userId) ||
            context.ShoppingLists.AsNoTracking().Any(item => item.UserId == userId) ||
            context.FavoriteMeals.AsNoTracking().Any(item => item.UserId == userId) ||
            context.FavoriteProducts.AsNoTracking().Any(item => item.UserId == userId) ||
            context.FavoriteRecipes.AsNoTracking().Any(item => item.UserId == userId) ||
            context.RecipeComments.AsNoTracking().Any(item => item.UserId == userId) ||
            context.RecipeLikes.AsNoTracking().Any(item => item.UserId == userId) ||
            context.ContentReports.AsNoTracking().Any(item => item.UserId == userId) ||
            context.DietologistInvitations.AsNoTracking().Any(item => item.ClientUserId == userId || item.DietologistUserId == userId) ||
            context.Recommendations.AsNoTracking().Any(item => item.ClientUserId == userId || item.DietologistUserId == userId) ||
            context.RecommendationComments.AsNoTracking().Any(item => item.AuthorUserId == userId)), cancellationToken);
}
