using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class UserRoleMembershipServiceTests {
    [Fact]
    public async Task EnsureRoleAsync_WithEmptyUserId_ThrowsArgumentException() {
        var service = new UserRoleMembershipService(context: null!);

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.EnsureRoleAsync(UserId.Empty, RoleNames.Premium, CancellationToken.None));

        Assert.Equal("userId", ex.ParamName);
    }

    [Fact]
    public async Task EnsureRoleAsync_WithBlankRoleName_ThrowsArgumentException() {
        var service = new UserRoleMembershipService(context: null!);

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.EnsureRoleAsync(UserId.New(), " ", CancellationToken.None));

        Assert.Equal("roleName", ex.ParamName);
    }

    [Fact]
    public async Task EnsureRoleAsync_WithInMemoryDatabase_AddsMissingRoleMembership() {
        await using FoodDiaryDbContext context = CreateContext();
        var role = Role.Create(RoleNames.Premium);
        User user = CreateUser();
        context.Roles.Add(role);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = new UserRoleMembershipService(context);

        await service.EnsureRoleAsync(user.Id, $" {RoleNames.Premium} ", CancellationToken.None);
        await context.SaveChangesAsync();

        UserRole userRole = Assert.Single(context.UserRoles);
        Assert.Multiple(
            () => Assert.Equal(user.Id, userRole.UserId),
            () => Assert.Equal(role.Id, userRole.RoleId),
            () => Assert.Equal(1, user.SecurityVersion));
    }

    [Fact]
    public async Task EnsureRoleAsync_WithInMemoryDatabase_WhenRoleDoesNotExist_DoesNotAddMembership() {
        await using FoodDiaryDbContext context = CreateContext();
        var service = new UserRoleMembershipService(context);

        await service.EnsureRoleAsync(UserId.New(), RoleNames.Premium, CancellationToken.None);

        Assert.Empty(context.UserRoles);
    }

    [Fact]
    public async Task EnsureRoleAsync_WithInMemoryDatabase_WhenMembershipExists_DoesNotDuplicateMembership() {
        await using FoodDiaryDbContext context = CreateContext();
        var role = Role.Create(RoleNames.Premium);
        User user = CreateUser();
        context.Roles.Add(role);
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole(user.Id, role.Id));
        await context.SaveChangesAsync();
        var service = new UserRoleMembershipService(context);

        await service.EnsureRoleAsync(user.Id, RoleNames.Premium, CancellationToken.None);

        Assert.Single(context.UserRoles);
        Assert.Equal(0, user.SecurityVersion);
    }

    [Fact]
    public async Task RemoveRoleAsync_WithInMemoryDatabase_RemovesExistingRoleMembership() {
        await using FoodDiaryDbContext context = CreateContext();
        var role = Role.Create(RoleNames.Premium);
        User user = CreateUser();
        context.Roles.Add(role);
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole(user.Id, role.Id));
        await context.SaveChangesAsync();
        var service = new UserRoleMembershipService(context);

        await service.RemoveRoleAsync(user.Id, $" {RoleNames.Premium} ", CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Empty(context.UserRoles);
        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task RemoveRoleAsync_WithInMemoryDatabase_WhenMembershipDoesNotExist_DoesNothing() {
        await using FoodDiaryDbContext context = CreateContext();
        var role = Role.Create(RoleNames.Premium);
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        var service = new UserRoleMembershipService(context);

        await service.RemoveRoleAsync(UserId.New(), RoleNames.Premium, CancellationToken.None);

        Assert.Empty(context.UserRoles);
    }

    [Fact]
    public async Task UserRoleCatalogService_EnsureRolesByNamesAsync_ReturnsExistingAndCreatesMissingRoles() {
        await using FoodDiaryDbContext context = CreateContext();
        var existingRole = Role.Create(RoleNames.Admin);
        context.Roles.Add(existingRole);
        await context.SaveChangesAsync();
        var service = new UserRoleCatalogService(context);

        IReadOnlyList<Role> roles = await service.EnsureRolesByNamesAsync([RoleNames.Admin, RoleNames.Premium], CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Multiple(
            () => Assert.Equal(2, roles.Count),
            () => Assert.Contains(roles, role => role.Id == existingRole.Id),
            () => Assert.Contains(roles, role => string.Equals(role.Name, RoleNames.Premium, StringComparison.Ordinal)),
            () => Assert.Equal(2, context.Roles.Count()));
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private static User CreateUser() => User.Create("security-version@example.com", "PasswordHash1!");
}
