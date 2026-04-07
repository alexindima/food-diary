using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence.Images;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class ImageAssetRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task MealAiSessionAsset_IsReportedInUse_AndExcludedFromUnusedCandidates() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("image-ai@example.com", "hash");
        var referencedAsset = ImageAsset.Create(user.Id, "images/ai-referenced.webp", "https://cdn.example.com/ai-referenced.webp");
        var unusedAsset = ImageAsset.Create(user.Id, "images/unused.webp", "https://cdn.example.com/unused.webp");

        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc));
        meal.AddAiSession(
            referencedAsset.Id,
            AiRecognitionSource.Photo,
            new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
            "analysis",
            []);

        context.Users.Add(user);
        context.ImageAssets.AddRange(referencedAsset, unusedAsset);
        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        var repository = new ImageAssetRepository(context);

        var isReferencedAssetInUse = await repository.IsAssetInUse(referencedAsset.Id, CancellationToken.None);
        var unusedCandidates = await repository.GetUnusedOlderThanAsync(
            DateTime.UtcNow.AddDays(1),
            batchSize: 10,
            CancellationToken.None);

        Assert.True(isReferencedAssetInUse);
        Assert.DoesNotContain(unusedCandidates, asset => asset.Id == referencedAsset.Id);
        Assert.Contains(unusedCandidates, asset => asset.Id == unusedAsset.Id);
    }
}
