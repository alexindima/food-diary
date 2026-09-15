using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.DailyAdvices.Infrastructure;
using FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedAdviceContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ModelContainsOnlyOwnedAdviceTable() {
        using var context = new DailyAdvicesDbContext(new DbContextOptionsBuilder<DailyAdvicesDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        IEntityType entity = Assert.Single(context.Model.GetEntityTypes());
        Assert.Multiple(
            () => Assert.Equal(typeof(DailyAdvice), entity.ClrType),
            () => Assert.Equal("DailyAdvices", entity.GetTableName()));
    }

    [RequiresDockerFact]
    public async Task SharedConnectionPreservesLocaleProjectionWithoutTrackingAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var englishAdvice = DailyAdvice.Create("Drink water", "en", weight: 3, tag: "hydration");
        var russianAdvice = DailyAdvice.Create("Russian advice", "ru", weight: 2);
        central.DailyAdvices.AddRange(englishAdvice, russianAdvice);
        await central.SaveChangesAsync();
        central.ChangeTracker.Clear();
        var services = new ServiceCollection();
        services.AddSingleton(central);
        services.AddSingleton<SharedPersistenceDbContext>(central);
        services.AddSingleton<FoodDiary.Persistence.Abstractions.IModuleContextFactory>(central);
        services.AddDailyAdvicesModule();
        await using ServiceProvider provider = services.BuildServiceProvider();
        DailyAdvicesDbContext owned = provider.GetRequiredService<DailyAdvicesDbContext>();
        IDailyAdviceReadModelRepository repository = provider.GetRequiredService<IDailyAdviceReadModelRepository>();
        Assert.Same(central.Database.GetDbConnection(), owned.Database.GetDbConnection());
        IReadOnlyList<DailyAdviceReadModel> englishResults = await repository.GetByLocaleReadModelsAsync(" ");
        Assert.All(englishResults, advice => Assert.Equal("en", advice.Locale));
        DailyAdviceReadModel english = Assert.Single(englishResults, advice => advice.Id == englishAdvice.Id.Value);
        Assert.Multiple(
            () => Assert.Equal("Drink water", english.Value),
            () => Assert.Equal("en", english.Locale),
            () => Assert.Equal("hydration", english.Tag),
            () => Assert.Equal(3, english.Weight));
        Assert.Equal("Russian advice", Assert.Single(await repository.GetByLocaleReadModelsAsync(" RU_ru "), advice => advice.Id == russianAdvice.Id.Value).Value);
        Assert.Equal("Russian advice", Assert.Single(await repository.GetByLocaleReadModelsAsync("ru-RU"), advice => advice.Id == russianAdvice.Id.Value).Value);
        Assert.Empty(await repository.GetByLocaleReadModelsAsync("de"));
        Assert.Empty(owned.ChangeTracker.Entries());
        Assert.Empty(central.ChangeTracker.Entries());
    }
}
