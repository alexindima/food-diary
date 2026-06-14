using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class EmailTemplateProviderTests {
    [Fact]
    public async Task GetActiveTemplateAsync_WhenLocaleMissing_UsesEnglishTemplate() {
        await using ServiceProvider provider = CreateProvider();
        await SeedAsync(
            provider,
            EmailTemplate.Create("verify_email", "en", "English subject", "<p>en</p>", "en", isActive: true));
        var templateProvider = new EmailTemplateProvider(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IMemoryCache>());

        EmailTemplateContent? template = await templateProvider.GetActiveTemplateAsync(
            " verify_EMAIL ",
            "   ",
            CancellationToken.None);

        Assert.NotNull(template);
        Assert.Equal("English subject", template.Subject);
    }

    [Fact]
    public async Task GetActiveTemplateAsync_WhenLocalizedTemplateMissing_FallsBackToEnglishTemplate() {
        await using ServiceProvider provider = CreateProvider();
        await SeedAsync(
            provider,
            EmailTemplate.Create("reset_password", "en", "Reset password", "<p>en</p>", "en", isActive: true));
        var templateProvider = new EmailTemplateProvider(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IMemoryCache>());

        EmailTemplateContent? template = await templateProvider.GetActiveTemplateAsync(
            "reset_password",
            "ru-RU",
            CancellationToken.None);

        Assert.NotNull(template);
        Assert.Equal("Reset password", template.Subject);
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

    private static async Task SeedAsync(ServiceProvider provider, params EmailTemplate[] templates) {
        AsyncServiceScope scope = provider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            context.EmailTemplates.AddRange(templates);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
