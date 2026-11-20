using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecipePhotoPublicationMigrationIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string BeforePrivateUploads = "20261119211057_AddRecipePublicationModeration";

    [RequiresDockerFact]
    public async Task Upgrade_PreservesConfirmedLegacyAssetsAsPublicAsync() {
        await using FoodDiaryDbContext database = databaseFixture.CreateDbContext(await databaseFixture.CreateIsolatedDatabaseAsync());
        await database.GetService<IMigrator>().MigrateAsync(BeforePrivateUploads);
        var author = User.Create("legacy-photo@example.test", "hash");
        database.Users.Add(author);
        await database.SaveChangesAsync();

        var legacy = ImageAsset.Create(author.Id, "legacy/cover.jpg", "https://cdn.example.com/legacy/cover.jpg");
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ImageAssets" ("Id", "UserId", "ObjectKey", "Url", "IsConfirmed", "CreatedOnUtc")
            VALUES ({legacy.Id.Value}, {author.Id.Value}, {legacy.ObjectKey}, {legacy.Url}, TRUE, {legacy.CreatedOnUtc})
            """, CancellationToken.None);

        await database.Database.MigrateAsync();
        ImageAsset migrated = await database.ImageAssets.AsNoTracking().SingleAsync(asset => asset.Id == legacy.Id);

        Assert.Multiple(
            () => Assert.True(migrated.IsConfirmed),
            () => Assert.False(migrated.IsPrivate),
            () => Assert.Equal(legacy.Url, migrated.Url),
            () => Assert.False(database.Database.HasPendingModelChanges()));
    }
}
