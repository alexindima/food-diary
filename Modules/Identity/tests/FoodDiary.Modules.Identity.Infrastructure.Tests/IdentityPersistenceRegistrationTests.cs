using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Infrastructure;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Authentication;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentityPersistenceRegistrationTests {
    [Fact]
    public void AddIdentityPersistence_ReadModelRepositorySharesScopedSessionRepository() {
        var services = new ServiceCollection();
        services.AddDbContext<FoodDiaryDbContext>();
        services.AddIdentityPersistence();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.Same(scope.ServiceProvider.GetRequiredService<IRefreshTokenSessionRepository>(),
            scope.ServiceProvider.GetRequiredService<IRefreshTokenSessionReadModelRepository>());
    }

    [Fact]
    public void AddIdentityPersistence_LoginEventAliasesShareOneInstancePerScope() {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        Assert.Same(services, services.AddIdentityPersistence());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IUserLoginEventRepository repository = first.ServiceProvider.GetRequiredService<IUserLoginEventRepository>();

        Assert.Multiple(
            () => Assert.IsType<UserLoginEventRepository>(repository),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserLoginEventReadRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserLoginEventWriteRepository>()),
            () => Assert.NotSame(repository, second.ServiceProvider.GetRequiredService<IUserLoginEventRepository>()),
            () => Assert.Same(typeof(IdentityModuleRegistration).Assembly, repository.GetType().Assembly));
    }

    [Fact]
    public void AddIdentityPersistence_TelegramReplayGuardIsScopedAndUsesModuleOwnedModel() {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<FoodDiaryDbContext>();
        Assert.Same(services, services.AddIdentityPersistence());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        ITelegramAssertionReplayGuard guard = first.ServiceProvider.GetRequiredService<ITelegramAssertionReplayGuard>();

        Assert.Multiple(
            () => Assert.IsType<TelegramAssertionReplayGuard>(guard),
            () => Assert.Same(guard, first.ServiceProvider.GetRequiredService<ITelegramAssertionReplayGuard>()),
            () => Assert.NotSame(guard, second.ServiceProvider.GetRequiredService<ITelegramAssertionReplayGuard>()),
            () => Assert.Same(typeof(IdentityModuleRegistration).Assembly, guard.GetType().Assembly),
            () => Assert.Same(typeof(IdentityPersistenceModelRegistration).Assembly, typeof(ConsumedTelegramAssertion).Assembly));
    }

    [Fact]
    public void AddIdentityPersistence_TemplateProviderRemainsSingletonAcrossScopes() {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddIdentityPersistence();
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IEmailTemplateProvider templates = provider.GetRequiredService<IEmailTemplateProvider>();

        Assert.Multiple(
            () => Assert.IsType<EmailTemplateProvider>(templates),
            () => Assert.Same(templates, first.ServiceProvider.GetRequiredService<IEmailTemplateProvider>()),
            () => Assert.Same(templates, second.ServiceProvider.GetRequiredService<IEmailTemplateProvider>()),
            () => Assert.Same(typeof(IdentityModuleRegistration).Assembly, templates.GetType().Assembly));
    }

    [Fact]
    public async Task RegisteredTemplateProvider_ReusesCachedTemplateAcrossDatabaseScopes() {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityPersistence();
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using (AsyncServiceScope seedScope = provider.CreateAsyncScope()) {
            FoodDiaryDbContext context = seedScope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            context.EmailTemplates.Add(EmailTemplate.Create("verify_email", "en", "Original", "<p>original</p>", "original", isActive: true));
            await context.SaveChangesAsync();
        }

        IEmailTemplateProvider templates = provider.GetRequiredService<IEmailTemplateProvider>();
        EmailTemplateContent? first = await templates.GetActiveTemplateAsync("verify_email", "en");
        Assert.NotNull(first);
        Assert.Equal("Original", first.Subject);
        await using (AsyncServiceScope updateScope = provider.CreateAsyncScope()) {
            FoodDiaryDbContext context = updateScope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            EmailTemplate stored = await context.EmailTemplates.SingleAsync();
            stored.Update("Changed", "<p>changed</p>", "changed", isActive: true);
            await context.SaveChangesAsync();
        }

        EmailTemplateContent? cached = await templates.GetActiveTemplateAsync(" VERIFY_EMAIL ", "en-US");
        Assert.Same(first, cached);
    }
}
