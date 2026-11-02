using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class PersonalDataArchiveBudgetTests {
    [Fact]
    public async Task Archive_StopsAtTheUncompressedBudgetEvenForHighlyCompressibleValidRecords() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new FoodDiaryDbContext(options);
        var owner = User.Create("large-archive@example.test", "synthetic-hash");
        context.Users.Add(owner);
        string description = new('a', Recipe.DescriptionMaxLength);
        string comment = new('b', Recipe.CommentMaxLength);
        string url = "https://example.test/" + new string('c', 2000);
        // The repeated ASCII field values alone exceed 64 MiB: 12,000 * (2,048 + 2,048 + 2,000).
        // Each field is valid individually; ZIP compression cannot bypass the plaintext budget.
        context.Recipes.AddRange(Enumerable.Range(0, 12_000)
            .Select(_ => Recipe.Create(owner.Id, "Large archive recipe", servings: 1, description: description, comment: comment, imageUrl: url)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var services = new ServiceCollection();
        services.AddSingleton<ICompositionReadContext>(context);
        services.AddSingleton(TimeProvider.System);
        services.AddReadModelComposition();
        await using ServiceProvider provider = services.BuildServiceProvider();

        await Assert.ThrowsAsync<PersonalDataArchiveLimitException>(() => provider.GetRequiredService<IPersonalDataArchiveReader>()
            .ReadArchiveAsync(owner.Id, CancellationToken.None));
    }
}
