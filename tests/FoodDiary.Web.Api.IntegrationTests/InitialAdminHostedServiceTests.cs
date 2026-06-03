using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class InitialAdminHostedServiceTests {
    [Fact]
    public async Task StartAsync_WhenPasswordIsBlank_DoesNotCreateUser() {
        using var provider = BuildServiceProvider();
        var service = CreateService(
            provider,
            new InitialAdminOptions {
                Email = "owner@fooddiary.test",
                Password = " "
            });

        await service.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        Assert.Empty(dbContext.Users);
    }

    [Fact]
    public async Task StartAsync_WhenUserAlreadyExists_DoesNotCreateDuplicate() {
        using var provider = BuildServiceProvider();
        using (var seedScope = provider.CreateScope()) {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            seedContext.Users.Add(User.Create("owner@fooddiary.test", "existing-hash"));
            await seedContext.SaveChangesAsync();
        }
        var service = CreateService(
            provider,
            new InitialAdminOptions {
                Email = "owner@fooddiary.test",
                Password = "StrongPassword123"
            });

        await service.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        Assert.Single(dbContext.Users);
    }

    [Fact]
    public async Task StartAsync_WhenConfigured_CreatesConfirmedAdminWithBootstrapRoles() {
        using var provider = BuildServiceProvider();
        using (var seedScope = provider.CreateScope()) {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            seedContext.Roles.Add(Role.Create(RoleNames.Owner));
            await seedContext.SaveChangesAsync();
        }
        var service = CreateService(
            provider,
            new InitialAdminOptions {
                Email = " owner@fooddiary.test ",
                Password = "StrongPassword123"
            });

        await service.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        var admin = await dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleAsync();
        var roles = await dbContext.Roles.Select(role => role.Name).ToListAsync();

        Assert.Equal("owner@fooddiary.test", admin.Email);
        Assert.True(admin.IsEmailConfirmed);
        Assert.Equal("test-hash:StrongPassword123", admin.Password);
        Assert.Contains(RoleNames.Owner, roles);
        Assert.Contains(RoleNames.Admin, roles);
        Assert.Contains(RoleNames.Premium, roles);
        Assert.Equal(
            [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium],
            admin.GetRoleNames());
    }

    private static InitialAdminHostedService CreateService(
        IServiceProvider serviceProvider,
        InitialAdminOptions options) =>
        new(
            serviceProvider,
            OptionsFactory.Create(options),
            NullLogger<InitialAdminHostedService>.Instance);

    private static ServiceProvider BuildServiceProvider() {
        var databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddDbContext<FoodDiaryDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<TestPasswordHasher>();
        services.AddSingleton<FoodDiary.Application.Abstractions.Authentication.Common.IPasswordHasher>(
            provider => provider.GetRequiredService<TestPasswordHasher>());
        return services.BuildServiceProvider();
    }
}
