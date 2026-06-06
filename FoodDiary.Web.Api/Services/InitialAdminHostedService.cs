using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Web.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Services;

public sealed class InitialAdminHostedService(
    IServiceProvider serviceProvider,
    IOptions<InitialAdminOptions> options,
    ILogger<InitialAdminHostedService> logger) : IHostedService {
    private static readonly string[] BootstrapRoles = [
        RoleNames.Owner,
        RoleNames.Admin,
        RoleNames.Premium,
    ];

    public async Task StartAsync(CancellationToken cancellationToken) {
        InitialAdminOptions settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Password)) {
            logger.LogInformation("Initial admin bootstrap skipped because InitialAdmin:Password is not configured.");
            return;
        }

        AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            IPasswordHasher passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            string normalizedEmail = settings.Email.Trim();

            if (await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken).ConfigureAwait(false)) {
                logger.LogInformation("Initial admin bootstrap skipped because user {Email} already exists.", normalizedEmail);
                return;
            }

            IReadOnlyList<Role> roles = await EnsureBootstrapRolesAsync(dbContext, cancellationToken).ConfigureAwait(false);
            var admin = User.Create(normalizedEmail, passwordHasher.Hash(settings.Password));
            admin.SetEmailConfirmed(isConfirmed: true);
            admin.ReplaceRoles(roles);

            await dbContext.Users.AddAsync(admin, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Initial admin user {Email} was created.", normalizedEmail);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<IReadOnlyList<Role>> EnsureBootstrapRolesAsync(
        FoodDiaryDbContext dbContext,
        CancellationToken cancellationToken) {
        List<Role> roles = await dbContext.Roles
            .Where(role => ((IEnumerable<string>)BootstrapRoles).Contains(role.Name))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var existingNames = roles.Select(role => role.Name).ToHashSet(StringComparer.Ordinal);
        foreach (string roleName in BootstrapRoles.Where(roleName => !existingNames.Contains(roleName))) {
            var role = Role.Create(roleName);
            roles.Add(role);
            await dbContext.Roles.AddAsync(role, cancellationToken).ConfigureAwait(false);
        }

        return roles;
    }
}
