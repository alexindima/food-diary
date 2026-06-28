using FoodDiary.Application;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class InitialAdminHostedServiceTests {
    [Fact]
    public async Task StartAsync_WhenPasswordIsBlank_DoesNotCreateUser() {
        await using ServiceProvider provider = BuildServiceProvider();
        InitialAdminHostedService service = CreateService(
            provider,
            new InitialAdminOptions {
                Email = "owner@fooddiary.test",
                Password = " ",
            });

        await service.StartAsync(CancellationToken.None);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        Assert.Empty(dbContext.Users);
    }

    [Fact]
    public async Task StartAsync_WhenUserAlreadyExists_DoesNotCreateDuplicate() {
        await using ServiceProvider provider = BuildServiceProvider();
        await using (AsyncServiceScope seedScope = provider.CreateAsyncScope()) {
            FoodDiaryDbContext seedContext = seedScope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            seedContext.Users.Add(User.Create("owner@fooddiary.test", "existing-hash"));
            await seedContext.SaveChangesAsync();
        }
        InitialAdminHostedService service = CreateService(
            provider,
            new InitialAdminOptions {
                Email = "owner@fooddiary.test",
                Password = "StrongPassword123",
            });

        await service.StartAsync(CancellationToken.None);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        Assert.Single(dbContext.Users);
    }

    [Fact]
    public async Task StartAsync_WhenConfigured_CreatesConfirmedAdminWithBootstrapRoles() {
        await using ServiceProvider provider = BuildServiceProvider();
        await using (AsyncServiceScope seedScope = provider.CreateAsyncScope()) {
            FoodDiaryDbContext seedContext = seedScope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            seedContext.Roles.Add(Role.Create(RoleNames.Owner));
            await seedContext.SaveChangesAsync();
        }
        InitialAdminHostedService service = CreateService(
            provider,
            new InitialAdminOptions {
                Email = " owner@fooddiary.test ",
                Password = "StrongPassword123",
            });

        await service.StartAsync(CancellationToken.None);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        User admin = await dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleAsync();
        List<string> roles = await dbContext.Roles.Select(role => role.Name).ToListAsync();

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
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddDbContext<FoodDiaryDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddLogging();
        services.AddApplication();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, TestUnitOfWork>();
        services.AddSingleton<TestPasswordHasher>();
        services.AddSingleton<FoodDiary.Application.Abstractions.Authentication.Common.IPasswordHasher>(
            provider => provider.GetRequiredService<TestPasswordHasher>());
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestUnitOfWork(FoodDiaryDbContext dbContext) : IUnitOfWork {
        public bool HasPendingChanges => dbContext.ChangeTracker.HasChanges();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            dbContext.SaveChangesAsync(cancellationToken);
    }
}
