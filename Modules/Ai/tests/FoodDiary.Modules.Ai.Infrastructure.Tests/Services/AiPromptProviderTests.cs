using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using System.Globalization;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class AiPromptProviderTests {
    [Fact]
    public async Task GetPromptAsync_WhenPromptIsCached_ReturnsCachedPrompt() {
        await using ServiceProvider provider = CreateProvider();
        IMemoryCache cache = provider.GetRequiredService<IMemoryCache>();
        cache.Set("ai-prompt:vision:ru", "cached prompt");
        IAiPromptProvider promptProvider = new AiPromptProvider(
            cache,
            provider.GetRequiredService<IServiceScopeFactory>());

        string prompt = await promptProvider.GetPromptAsync("vision", "ru", CancellationToken.None);

        Assert.Equal("cached prompt", prompt);
    }

    [Fact]
    public async Task GetPromptAsync_WhenActiveTemplateExists_ReturnsRequestedLocaleAndCachesIt() {
        await using ServiceProvider provider = CreateProvider();
        await SeedAsync(
            provider,
            CreateTemplate("vision", "English prompt", versionUpdates: 5),
            CreateTemplate("vision", "new prompt", versionUpdates: 1, locale: "ru"));
        IMemoryCache cache = provider.GetRequiredService<IMemoryCache>();
        IAiPromptProvider promptProvider = new AiPromptProvider(
            cache,
            provider.GetRequiredService<IServiceScopeFactory>());

        string prompt = await promptProvider.GetPromptAsync("vision", "ru", CancellationToken.None);
        bool cached = cache.TryGetValue("ai-prompt:vision:ru", out string? cachedPrompt);

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

        string prompt = await promptProvider.GetPromptAsync(key, "ru", CancellationToken.None);

        Assert.Contains(expectedText, prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(" RU ", "Russian")]
    [InlineData("ru-RU", "Russian")]
    [InlineData("en", "English")]
    [InlineData(null, "English")]
    [InlineData("fr", "English")]
    public async Task GetPromptAsync_IsolatesCachedLocales(string? language, string expected) {
        await using ServiceProvider provider = CreateProvider();
        await SeedAsync(provider,
            AiPromptTemplate.Create("vision", "en", "English"),
            AiPromptTemplate.Create("vision", "ru", "Russian"));
        IAiPromptProvider prompts = new AiPromptProvider(
            provider.GetRequiredService<IMemoryCache>(), provider.GetRequiredService<IServiceScopeFactory>());

        Assert.Equal("English", await prompts.GetPromptAsync("vision", "en", CancellationToken.None));
        Assert.Equal("Russian", await prompts.GetPromptAsync("vision", "ru", CancellationToken.None));
        Assert.Equal(expected, await prompts.GetPromptAsync("vision", language, CancellationToken.None));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPromptAsync_WhenRequestedLocaleInactive_UsesEnglishOrBuiltIn(bool englishActive) {
        await using ServiceProvider provider = CreateProvider();
        await SeedAsync(provider,
            AiPromptTemplate.Create("nutrition", "en", "English", isActive: englishActive),
            AiPromptTemplate.Create("nutrition", "ru", "Inactive Russian", isActive: false));
        IAiPromptProvider prompts = new AiPromptProvider(
            provider.GetRequiredService<IMemoryCache>(), provider.GetRequiredService<IServiceScopeFactory>());

        string result = await prompts.GetPromptAsync("nutrition", "ru", CancellationToken.None);

        if (englishActive) {
            Assert.Equal("English", result);
        } else {
            Assert.Contains("nutrition assistant", result, StringComparison.Ordinal);
        }
    }

    private static ServiceProvider CreateProvider() {
        var services = new ServiceCollection();
        DbContextOptions<AiDbContext> options = new DbContextOptionsBuilder<AiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        services.AddMemoryCache();
        services.AddSingleton(new AiDbContext(options));
        return services.BuildServiceProvider();
    }

    private static AiPromptTemplate CreateTemplate(string key, string promptText, int versionUpdates, string locale = "en") {
        var template = AiPromptTemplate.Create(key, locale, "initial prompt");
        for (int i = 0; i < versionUpdates; i++) {
            template.Update($"{promptText}-{i.ToString(CultureInfo.InvariantCulture)}");
        }

        template.Update(promptText);
        return template;
    }

    private static async Task SeedAsync(ServiceProvider provider, params AiPromptTemplate[] templates) {
        AsyncServiceScope scope = provider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            AiDbContext context = scope.ServiceProvider.GetRequiredService<AiDbContext>();
            context.AiPromptTemplates.AddRange(templates);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
