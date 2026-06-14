using System.Globalization;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class AiPromptProviderTests {
    [Fact]
    public async Task GetPromptAsync_WhenPromptIsCached_ReturnsCachedPrompt() {
        await using ServiceProvider provider = CreateProvider();
        IMemoryCache cache = provider.GetRequiredService<IMemoryCache>();
        cache.Set("ai-prompt:vision", "cached prompt");
        IAiPromptProvider promptProvider = new AiPromptProvider(
            cache,
            provider.GetRequiredService<IServiceScopeFactory>());

        string prompt = await promptProvider.GetPromptAsync("vision", CancellationToken.None);

        Assert.Equal("cached prompt", prompt);
    }

    [Fact]
    public async Task GetPromptAsync_WhenActiveTemplateExists_ReturnsLatestPromptAndCachesIt() {
        await using ServiceProvider provider = CreateProvider();
        await SeedAsync(
            provider,
            CreateTemplate("vision", "old prompt", versionUpdates: 0),
            CreateTemplate("vision", "new prompt", versionUpdates: 1));
        IMemoryCache cache = provider.GetRequiredService<IMemoryCache>();
        IAiPromptProvider promptProvider = new AiPromptProvider(
            cache,
            provider.GetRequiredService<IServiceScopeFactory>());

        string prompt = await promptProvider.GetPromptAsync("vision", CancellationToken.None);
        bool cached = cache.TryGetValue("ai-prompt:vision", out string? cachedPrompt);

        Assert.Equal("new prompt", prompt);
        Assert.True(cached);
        Assert.Equal("new prompt", cachedPrompt);
    }

    [Theory]
    [InlineData("nutrition", "nutrition assistant")]
    [InlineData("unknown-key", "unknown-key")]
    public async Task GetPromptAsync_WhenTemplateMissing_ReturnsFallbackOrKey(string key, string expectedText) {
        await using ServiceProvider provider = CreateProvider();
        IAiPromptProvider promptProvider = new AiPromptProvider(
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<IServiceScopeFactory>());

        string prompt = await promptProvider.GetPromptAsync(key, CancellationToken.None);

        Assert.Contains(expectedText, prompt, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider CreateProvider() {
        var services = new ServiceCollection();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        services.AddMemoryCache();
        services.AddSingleton(new FoodDiaryDbContext(options));
        return services.BuildServiceProvider();
    }

    private static AiPromptTemplate CreateTemplate(string key, string promptText, int versionUpdates) {
        var template = AiPromptTemplate.Create(key, "en", "initial prompt");
        for (int i = 0; i < versionUpdates; i++) {
            template.Update($"{promptText}-{i.ToString(CultureInfo.InvariantCulture)}");
        }

        template.Update(promptText);
        return template;
    }

    private static async Task SeedAsync(ServiceProvider provider, params AiPromptTemplate[] templates) {
        AsyncServiceScope scope = provider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            context.AiPromptTemplates.AddRange(templates);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
