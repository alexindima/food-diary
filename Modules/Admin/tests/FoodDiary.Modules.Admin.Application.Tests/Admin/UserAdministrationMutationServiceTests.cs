using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class UserAdministrationMutationServiceTests {
    [Fact]
    public async Task CreateAsync_WhenEmailExists_ReturnsConflictWithoutWriting() {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        IUserRoleCatalogService roles = Substitute.For<IUserRoleCatalogService>();
        lookup.GetByEmailIncludingDeletedAsync("admin@example.com", Arg.Any<CancellationToken>())
            .Returns(User.Create("admin@example.com", "hash"));

        Result<UserAdminReadModel> result = await CreateService(lookup, writer, roles)
            .CreateAsync(CreateRequest(), CancellationToken.None);

        ResultAssert.Failure(result, "User.EmailAlreadyExists");
        await writer.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task CreateAsync_WhenRoleIsNotConfigured_ReturnsValidationFailureWithoutWriting() {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        IUserRoleCatalogService roles = Substitute.For<IUserRoleCatalogService>();
        lookup.GetByEmailIncludingDeletedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        roles.GetRolesByNamesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Role>>([]);

        Result<UserAdminReadModel> result = await CreateService(lookup, writer, roles)
            .CreateAsync(CreateRequest(), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await writer.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesUserAndRoleAudit() {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        IUserRoleCatalogService roles = Substitute.For<IUserRoleCatalogService>();
        var adminRole = Role.Create(RoleNames.Admin);
        lookup.GetByEmailIncludingDeletedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        roles.GetRolesByNamesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Role>>([adminRole]);
        writer.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<User>());
        User? writtenUser = null;
        IReadOnlyCollection<UserRoleAuditEvent>? writtenAudit = null;
        writer.UpdateAsync(
                Arg.Do<User>(user => writtenUser = user),
                Arg.Do<IReadOnlyCollection<UserRoleAuditEvent>>(events => writtenAudit = events),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        UserAdminReadModel model = ResultAssert.Success(await CreateService(lookup, writer, roles)
            .CreateAsync(CreateRequest(), CancellationToken.None));

        Assert.NotNull(writtenUser);
        UserRoleAuditEvent audit = Assert.Single(writtenAudit!);
        Assert.Multiple(
            () => Assert.Equal("admin@example.com", model.Email),
            () => Assert.Equal("Alex", model.FirstName),
            () => Assert.Equal("User", model.LastName),
            () => Assert.True(model.IsEmailConfirmed),
            () => Assert.True(model.MustChangePassword),
            () => Assert.Equal("hashed:Temporary123!", writtenUser.Password),
            () => Assert.Equal(RoleNames.Admin, audit.RoleName),
            () => Assert.Equal("AdminUserCreator", audit.Source));
    }

    private static UserAdministrationMutationService CreateService(
        IUserLookupRepository lookup,
        IUserWriteRepository writer,
        IUserRoleCatalogService roles) => new(lookup, writer, roles, new PrefixPasswordHasher());

    private static UserAdminCreateModel CreateRequest() => new(
        "admin@example.com",
        "Alex",
        "User",
        "en",
        [RoleNames.Admin],
        "Temporary123!",
        IsEmailConfirmed: true,
        RequirePasswordChange: true,
        UserId.New(),
        new DateTime(2026, 8, 14, 10, 0, 0, DateTimeKind.Utc));

    [ExcludeFromCodeCoverage]
    private sealed class PrefixPasswordHasher : IPasswordHasher {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hashedPassword) =>
            string.Equals(Hash(password), hashedPassword, StringComparison.Ordinal);
    }
}
