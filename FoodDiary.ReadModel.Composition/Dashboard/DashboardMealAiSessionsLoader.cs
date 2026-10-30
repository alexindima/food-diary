using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Dashboard;

internal sealed class DashboardMealAiSessionsLoader(ICompositionReadContext context) {
    public async Task<ILookup<MealId, DashboardMealAiSessionReadModel>> LoadAsync(
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken) {
        var sessions = await (
            from session in context.MealAiSessions.AsNoTracking()
            join imageAsset in context.ImageAssets.AsNoTracking()
                on session.ImageAssetId equals (ImageAssetId?)imageAsset.Id into imageAssets
            from imageAsset in imageAssets.DefaultIfEmpty()
            where mealIds.Contains(session.MealId)
            select new {
                Session = new DashboardMealAiSessionProjection(
                    session.MealId,
                    session.Id,
                    session.Id.Value,
                    session.MealId.Value,
                    session.ImageAssetId.HasValue ? session.ImageAssetId.Value.Value : null,
                    imageAsset == null ? null : imageAsset.Url,
                    session.Source.ToString(),
                    session.Status.ToString(),
                    session.RecognizedAtUtc,
                    session.Notes),
                Items = context.MealAiItems.AsNoTracking()
                    .Where(item => item.MealAiSessionId == session.Id)
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
                    .ToList(),
            })
            .AsSingleQuery()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return sessions
            .OrderBy(row => row.Session.RecognizedAtUtc)
            .Select(row => ToReadModel(row.Session, row.Items))
            .ToLookup(session => new MealId(session.MealId));
    }

    private static DashboardMealAiSessionReadModel ToReadModel(
        DashboardMealAiSessionProjection session,
        IReadOnlyList<DashboardMealAiItemReadModel> items) {
        return new DashboardMealAiSessionReadModel(
            session.Id,
            session.MealIdValue,
            session.ImageAssetId,
            session.ImageUrl,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            items);
    }
}
