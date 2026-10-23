using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Modules.Users.Application.Commands.UpdateUserByAdministrator;
using FoodDiary.Modules.Users.Contracts.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Modules.Users.Contracts.Commands.UpdateUserByAdministrator;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class SystemContentOwnerAdministrationTests {
    [Fact]
    public async Task Update_DoesNotExposeOrActivateTheSystemOwnerThroughHumanAdministration() {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        IUserRoleCatalogService roles = Substitute.For<IUserRoleCatalogService>();
        var handler = new UpdateUserByAdministratorCommandHandler(lookup, writer, roles);
        var update = new UserAdminUpdateModel(SystemContentOwner.Id, IsActive: true, IsEmailConfirmed: true,
            Roles: ["Admin"], Language: "en", AiInputTokenLimit: null, AiOutputTokenLimit: null,
            ActorUserId: UserId.New(), UpdatedAtUtc: DateTime.UtcNow);

        Result<UserAdminReadModel> result = await handler.Handle(new UpdateUserByAdministratorCommand(update), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Empty(lookup.ReceivedCalls());
        Assert.Empty(writer.ReceivedCalls());
        Assert.Empty(roles.ReceivedCalls());
    }

    [Fact]
    public async Task SetPassword_DoesNotCreateCredentialsForTheSystemOwner() {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        IPasswordHasher passwords = Substitute.For<IPasswordHasher>();
        var handler = new SetUserPasswordByAdministratorCommandHandler(lookup, writer, passwords);

        Result result = await handler.Handle(new SetUserPasswordByAdministratorCommand(
            SystemContentOwner.Id, UserId.New(), "new-password"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Empty(lookup.ReceivedCalls());
        Assert.Empty(writer.ReceivedCalls());
        Assert.Empty(passwords.ReceivedCalls());
    }
}
