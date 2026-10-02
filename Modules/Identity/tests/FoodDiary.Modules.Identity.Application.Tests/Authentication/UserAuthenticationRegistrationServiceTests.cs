using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class UserAuthenticationRegistrationServiceTests {
    [Fact]
    public async Task RegisterAsync_ActiveAndDeletedAccounts_ReturnIdenticalErrorsWithoutIssuingIdentity() {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        IUserRoleCatalogService roles = Substitute.For<IUserRoleCatalogService>();
        var active = User.Create("existing@example.com", "hash");
        var deleted = User.Create("existing@example.com", "hash");
        deleted.DeleteAccount(DateTime.UtcNow);
        lookup.GetByEmailIncludingDeletedAsync("existing@example.com", Arg.Any<CancellationToken>()).Returns(active, deleted);
        UserAuthenticationRegistrationService service = CreateService(lookup, writer, roles);
        var registration = new UserRegistrationModel("existing@example.com", "password", "en", "token", DateTime.UtcNow.AddHours(24), DateTime.UtcNow);

        Result<UserAuthenticationPrincipalModel> activeResult = await service.RegisterAsync(registration, CancellationToken.None);
        Result<UserAuthenticationPrincipalModel> deletedResult = await service.RegisterAsync(registration, CancellationToken.None);

        Assert.True(activeResult.IsFailure);
        Assert.True(deletedResult.IsFailure);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(activeResult.Error), System.Text.Json.JsonSerializer.Serialize(deletedResult.Error));
        await writer.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
    [Fact]
    public async Task BootstrapInitialAdminAsync_WhenUserExists_DoesNotCreateAnotherUser() {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        IUserRoleCatalogService roles = Substitute.For<IUserRoleCatalogService>();
        lookup.GetByEmailIncludingDeletedAsync("owner@example.com", Arg.Any<CancellationToken>())
            .Returns(User.Create("owner@example.com", "hash"));

        UserInitialAdminBootstrapModel result = await CreateService(lookup, writer, roles)
            .BootstrapInitialAdminAsync(" owner@example.com ", "password", [RoleNames.Owner], CancellationToken.None);

        Assert.Multiple(
            () => Assert.False(result.Created),
            () => Assert.Equal("owner@example.com", result.Email));
        await writer.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BootstrapInitialAdminAsync_WhenUserIsMissing_CreatesConfirmedOwner() {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        IUserRoleCatalogService roles = Substitute.For<IUserRoleCatalogService>();
        lookup.GetByEmailIncludingDeletedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        roles.EnsureRolesByNamesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Role>>([Role.Create(RoleNames.Owner), Role.Create(RoleNames.Admin)]);
        User? added = null;
        writer.AddAsync(Arg.Do<User>(user => added = user), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<User>());

        UserInitialAdminBootstrapModel result = await CreateService(lookup, writer, roles)
            .BootstrapInitialAdminAsync(
                " owner@example.com ",
                "password",
                [RoleNames.Owner, RoleNames.Admin],
                CancellationToken.None);

        Assert.NotNull(added);
        Assert.Multiple(
            () => Assert.True(result.Created),
            () => Assert.Equal("owner@example.com", result.Email),
            () => Assert.Equal("owner@example.com", added.Email),
            () => Assert.True(added.IsEmailConfirmed),
            () => Assert.Equal("hashed:password", added.Password),
            () => Assert.Equal(2, added.UserRoles.Count));
    }

    private static UserAuthenticationRegistrationService CreateService(
        IUserLookupRepository lookup,
        IUserWriteRepository writer,
        IUserRoleCatalogService roles) => new(lookup, writer, roles, new PrefixPasswordHasher());

    [ExcludeFromCodeCoverage]
    private sealed class PrefixPasswordHasher : IPasswordHasher {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hashedPassword) =>
            string.Equals(Hash(password), hashedPassword, StringComparison.Ordinal);
    }
}
