using FoodDiary.Modules.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Admin.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminPersistenceRegistrationTests {
    [Fact]
    public void ImpersonationWriter_ResolvesWithoutReadComposition() {
        var services = new ServiceCollection();
        services.AddAdminPersistence();
        services.AddScoped(static _ => new AdminDbContext(new DbContextOptionsBuilder<AdminDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IAdminImpersonationSessionWriteRepository writer = first.ServiceProvider.GetRequiredService<IAdminImpersonationSessionWriteRepository>();
        Assert.IsType<AdminImpersonationSessionRepository>(writer);
        Assert.Null(first.ServiceProvider.GetService<IAdminImpersonationSessionQuery>());
        Assert.Same(writer, first.ServiceProvider.GetRequiredService<IAdminImpersonationSessionWriteRepository>());
        Assert.NotSame(writer, second.ServiceProvider.GetRequiredService<IAdminImpersonationSessionWriteRepository>());
    }

    [Fact]
    public void AddAdminPersistence_RoleAuditAliasesShareOneComposedInstancePerScope() {
        var services = new ServiceCollection();
        services.AddDbContext<FoodDiaryDbContext>();
        Assert.Same(services, services.AddAdminPersistence().AddReadModelComposition());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IAdminUserRoleAuditRepository repository = first.ServiceProvider.GetRequiredService<IAdminUserRoleAuditRepository>();

        Assert.Multiple(
            () => Assert.IsType<AdminUserRoleAuditRepository>(repository),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IAdminUserRoleAuditQuery>()),
            () => Assert.NotSame(repository, second.ServiceProvider.GetRequiredService<IAdminUserRoleAuditRepository>()),
            () => Assert.Same(typeof(ReadModelCompositionRegistration).Assembly, repository.GetType().Assembly));
    }
}
