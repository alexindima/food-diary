using FoodDiary.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.DataProtection;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Infrastructure.Persistence.Admin;
using FoodDiary.Infrastructure.Persistence.Authentication;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Infrastructure;

public static class IdentityModuleRegistration {
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services) {
        services.AddScoped(provider => provider.GetRequiredService<FoodDiaryDbContext>()
            .CreateModuleContext<IdentityDbContext>(options => new IdentityDbContext(options)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, IdentityUserDataPurgeParticipant>());
        services.AddDataProtection();
        services.AddScoped<ITelegramAssertionReplayGuard>(provider => new TelegramAssertionReplayGuard(
            provider.GetRequiredService<IdentityDbContext>(), provider.GetRequiredService<TimeProvider>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<ITelegramLoginTicketStore>(provider => new TelegramLoginTicketStore(
            provider.GetRequiredService<IdentityDbContext>(), provider.GetRequiredService<IDataProtectionProvider>(), provider.GetRequiredService<TimeProvider>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<ITelegramOperationStore>(provider => new TelegramOperationStore(
            provider.GetRequiredService<IdentityDbContext>(), provider.GetRequiredService<IDataProtectionProvider>(), provider.GetRequiredService<TimeProvider>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserLoginEventRepository>(provider => new UserLoginEventRepository(
            provider.GetRequiredService<IdentityDbContext>().UserLoginEvents, provider.GetRequiredService<IUserLoginEventQuery>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserLoginEventReadRepository>(static provider => provider.GetRequiredService<IUserLoginEventRepository>());
        services.AddScoped<IUserLoginEventWriteRepository>(static provider => provider.GetRequiredService<IUserLoginEventRepository>());
        services.AddSingleton<IEmailTemplateProvider, EmailTemplateProvider>();
        services.AddScoped<IRefreshTokenSessionRepository>(provider => new RefreshTokenSessionRepository(
            provider.GetRequiredService<IdentityDbContext>().UserRefreshTokenSessions, provider.GetRequiredService<IdentityDbContext>().Database, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserSessionRevocationService>(static provider => (RefreshTokenSessionRepository)provider.GetRequiredService<IRefreshTokenSessionRepository>());
        services.AddScoped<IRefreshTokenSessionReadModelRepository>(static provider => (RefreshTokenSessionRepository)provider.GetRequiredService<IRefreshTokenSessionRepository>());
        services.AddScoped<IRefreshTokenSessionReadRepository>(static provider => provider.GetRequiredService<IRefreshTokenSessionRepository>());
        services.AddScoped<IRefreshTokenSessionWriteRepository>(static provider => provider.GetRequiredService<IRefreshTokenSessionRepository>());
        services.AddScoped<IEmailTemplateRepository>(static provider =>
            new EmailTemplateRepository(provider.GetRequiredService<IdentityDbContext>().EmailTemplates, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IEmailTemplateReadRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());
        services.AddScoped<IEmailTemplateReadModelRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());
        services.AddScoped<IEmailTemplateWriteRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());
        return services;
    }

    private static Func<CancellationToken, Task> CreateTransactionSynchronizer(IServiceProvider provider) {
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        IdentityDbContext owned = provider.GetRequiredService<IdentityDbContext>();
        return async cancellationToken => {
            if (owned.Database.IsRelational()) {
                await owned.Database.UseTransactionAsync(shared.Database.CurrentTransaction?.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
            }
        };
    }
}
