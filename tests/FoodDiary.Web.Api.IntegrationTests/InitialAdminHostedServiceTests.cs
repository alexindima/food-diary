using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using FoodDiary.Mediator;
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

    [Fact]
    public async Task StartAsync_WhenBootstrapFails_LogsAndReturns() {
        var services = new ServiceCollection();
        services.AddScoped<ISender>(_ => new FailingSender());
        await using ServiceProvider provider = services.BuildServiceProvider();
        InitialAdminHostedService service = CreateService(
            provider,
            new InitialAdminOptions {
                Email = " owner@fooddiary.test ",
                Password = "StrongPassword123",
            });

        await service.StartAsync(CancellationToken.None);
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
        services.AddScoped<IUserLookupRepository>(static provider => provider.GetRequiredService<IUserRepository>());
        services.AddScoped<IUserWriteRepository>(static provider => provider.GetRequiredService<IUserRepository>());
        services.AddScoped<IUserRoleCatalogService, TestUserRoleCatalogService>();
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

    [ExcludeFromCodeCoverage]
    private sealed class TestUserRoleCatalogService(FoodDiaryDbContext dbContext) : IUserRoleCatalogService {
        public async Task<IReadOnlyList<Role>> GetRolesByNamesAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default) =>
            await dbContext.Roles
                .Where(role => names.Contains(role.Name))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        public async Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<Role> existingRoles = await GetRolesByNamesAsync(names, cancellationToken).ConfigureAwait(false);
            var existingNames = existingRoles.Select(role => role.Name).ToHashSet(StringComparer.Ordinal);
            List<Role> roles = [.. existingRoles];

            foreach (string roleName in names.Where(name => !existingNames.Contains(name))) {
                var role = Role.Create(roleName);
                roles.Add(role);
                await dbContext.Roles.AddAsync(role, cancellationToken).ConfigureAwait(false);
            }

            return roles;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingSender : ISender {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            if (request is BootstrapInitialAdminCommand) {
                object result = Result.Failure<BootstrapInitialAdminModel>(Errors.Validation.Invalid("InitialAdmin", "failed"));
                return Task.FromResult((TResponse)result);
            }

            throw new NotSupportedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
