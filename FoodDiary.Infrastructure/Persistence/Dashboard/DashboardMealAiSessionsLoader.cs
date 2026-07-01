using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardMealAiSessionsLoader(FoodDiaryDbContext context) {
    public async Task<ILookup<MealId, DashboardMealAiSessionReadModel>> LoadAsync(
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken) {
        List<DashboardMealAiSessionProjection> sessions = await context.MealAiSessions
            .AsNoTracking()
            .Where(session => mealIds.Contains(session.MealId))
            .Select(session => new DashboardMealAiSessionProjection(
                session.MealId,
                session.Id,
                session.Id.Value,
                session.MealId.Value,
                session.ImageAssetId.HasValue ? session.ImageAssetId.Value.Value : null,
                session.ImageAsset == null ? null : session.ImageAsset.Url,
                session.Source.ToString(),
                session.Status.ToString(),
                session.RecognizedAtUtc,
                session.Notes))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        MealAiSessionId[] sessionIds = [.. sessions.Select(session => session.SessionId)];
        ILookup<MealAiSessionId, DashboardMealAiItemReadModel> itemsBySessionId = await LoadAiItemsAsync(sessionIds, cancellationToken).ConfigureAwait(false);

        return sessions
            .OrderBy(session => session.RecognizedAtUtc)
            .Select(session => ToReadModel(session, itemsBySessionId))
            .ToLookup(session => new MealId(session.MealId));
    }

    private async Task<ILookup<MealAiSessionId, DashboardMealAiItemReadModel>> LoadAiItemsAsync(
        IReadOnlyCollection<MealAiSessionId> sessionIds,
        CancellationToken cancellationToken) {
        if (sessionIds.Count == 0) {
            return Array.Empty<DashboardMealAiItemReadModel>().ToLookup(item => new MealAiSessionId(item.SessionId));
        }

        List<DashboardMealAiItemReadModel> items = await context.MealAiItems
            .AsNoTracking()
            .Where(item => sessionIds.Contains(item.MealAiSessionId))
            .OrderBy(item => item.Id)
            .Select(item => new DashboardMealAiItemReadModel(
                item.Id.Value,
                item.MealAiSessionId.Value,
                item.NameEn,
                item.NameLocal,
                item.Amount,
                item.Unit,
                item.Calories,
                item.Proteins,
                item.Fats,
                item.Carbs,
                item.Fiber,
                item.Alcohol,
                item.Confidence,
                item.Resolution.ToString()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items.ToLookup(item => new MealAiSessionId(item.SessionId));
    }

    private static DashboardMealAiSessionReadModel ToReadModel(
        DashboardMealAiSessionProjection session,
        ILookup<MealAiSessionId, DashboardMealAiItemReadModel> itemsBySessionId) {
        return new DashboardMealAiSessionReadModel(
            session.Id,
            session.MealIdValue,
            session.ImageAssetId,
            session.ImageUrl,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. itemsBySessionId[session.SessionId]]);
    }
}
