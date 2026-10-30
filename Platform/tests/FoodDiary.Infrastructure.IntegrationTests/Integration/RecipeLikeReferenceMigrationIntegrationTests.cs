using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecipeLikeReferenceMigrationIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string Baseline = "20261017130323_InitialBaseline";

    [RequiresDockerFact]
    public async Task Upgrade_RemovesOnlyOrphanLikes_EnforcesRecipeExistenceAndCascadesRecipeDeletion() {
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(await databaseFixture.CreateIsolatedDatabaseAsync());
        await context.GetService<IMigrator>().MigrateAsync(Baseline);
        UserId authorId = await InitialBaselineUserFixture.InsertAsync(context, "recipe-like-author@example.test");
        UserId readerId = await InitialBaselineUserFixture.InsertAsync(context, "recipe-like-reader@example.test");
        var recipe = Recipe.Create(authorId, "Liked recipe", 1);
        var otherRecipe = Recipe.Create(authorId, "Other liked recipe", 1);
        var valid = RecipeLike.Create(readerId, recipe.Id);
        var otherValid = RecipeLike.Create(readerId, otherRecipe.Id);
        var orphan = RecipeLike.Create(readerId, RecipeId.New());
        context.Recipes.AddRange(recipe, otherRecipe);
        context.RecipeLikes.AddRange(valid, otherValid, orphan);
        await context.SaveChangesAsync();
        string[] validRowsBefore = await ReadLikesAsync(context, valid.Id.Value, otherValid.Id.Value);
        Assert.Equal(3, await context.RecipeLikes.CountAsync());

        await context.Database.MigrateAsync();
        context.ChangeTracker.Clear();

        Assert.Multiple(
            () => Assert.False(context.Database.HasPendingModelChanges()),
            () => Assert.Equal(2, validRowsBefore.Length));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        Assert.Equal(validRowsBefore, await ReadLikesAsync(context, valid.Id.Value, otherValid.Id.Value), StringComparer.Ordinal);
        Assert.Equal(2, await context.RecipeLikes.CountAsync());
        Assert.False(await context.RecipeLikes.AnyAsync(like => like.Id == orphan.Id));

        context.RecipeLikes.Add(RecipeLike.Create(readerId, RecipeId.New()));
        DbUpdateException rejected = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        PostgresException databaseError = Assert.IsType<PostgresException>(rejected.InnerException);
        Assert.Multiple(
            () => Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, databaseError.SqlState),
            () => Assert.Equal("FK_RecipeLikes_Recipes_RecipeId", databaseError.ConstraintName));
        context.ChangeTracker.Clear();

        Assert.Equal(1, await context.Recipes.Where(item => item.Id == recipe.Id).ExecuteDeleteAsync());

        RecipeLike remaining = Assert.Single(await context.RecipeLikes.AsNoTracking().ToListAsync());
        Assert.Multiple(
            () => Assert.Equal(otherValid.Id, remaining.Id),
            () => Assert.Equal(otherRecipe.Id, remaining.RecipeId),
            () => Assert.Equal(readerId, remaining.UserId));
        Assert.Equal(2, await context.Users.CountAsync());
        Assert.True(await context.Recipes.AnyAsync(item => item.Id == otherRecipe.Id));
    }

    private static Task<string[]> ReadLikesAsync(FoodDiaryDbContext context, Guid firstId, Guid secondId) =>
        context.Database.SqlQuery<string>($"""
            SELECT to_jsonb(like_row)::text AS "Value"
            FROM "RecipeLikes" AS like_row
            WHERE like_row."Id" = {firstId} OR like_row."Id" = {secondId}
            """).OrderBy(value => value).ToArrayAsync();
}
