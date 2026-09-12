using FoodDiary.ReadModel.Composition;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Infrastructure;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Admin.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminPersistenceRegistrationTests {
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
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IAdminUserRoleAuditReadRepository>()),
            () => Assert.NotSame(repository, second.ServiceProvider.GetRequiredService<IAdminUserRoleAuditRepository>()),
            () => Assert.Same(typeof(ReadModelCompositionRegistration).Assembly, repository.GetType().Assembly));
    }
}
