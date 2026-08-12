using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Admin.Commands.CreateAdminUser;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class CreateAdminUserCommandHandlerTests {
    [Fact]
    public async Task Handle_WithGeneratedPassword_CreatesConfirmedUserAndQueuesCredentialsEmail() {
        IUserAdministrationMutationService userManagementService = Substitute.For<IUserAdministrationMutationService>();
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        IAuditLogger auditLogger = Substitute.For<IAuditLogger>();
        UserAdminCreateModel? capturedRequest = null;
        userManagementService.CreateAsync(
                Arg.Do<UserAdminCreateModel>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(ToReadModel(call.Arg<UserAdminCreateModel>())));

        var handler = new CreateAdminUserCommandHandler(
            userManagementService,
            emailSender,
            auditLogger,
            TimeProvider.System);

        Result<AdminUserCreationModel> result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        ResultAssert.Success(result);
        UserAdminCreateModel request = Assert.IsType<UserAdminCreateModel>(capturedRequest);
        Assert.True(request.IsEmailConfirmed);
        Assert.True(request.RequirePasswordChange);
        Assert.Contains(RoleNames.Dietologist, request.Roles, StringComparer.Ordinal);
        AdminUserCreationModel creation = result.Value;
        Assert.NotEmpty(creation.TemporaryPassword);
        Assert.True(creation.CredentialsEmailQueued);
        await emailSender.Received(1).SendAccountCreatedAsync(
            Arg.Is<AccountCreatedMessage>((AccountCreatedMessage message) =>
                string.Equals(message.ToEmail, "dietologist@example.com", StringComparison.Ordinal) &&
                string.Equals(message.TemporaryPassword, creation.TemporaryPassword, StringComparison.Ordinal) &&
                string.Equals(message.Language, "ru", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        await userManagementService.Received(1).CreateAsync(Arg.Any<UserAdminCreateModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsConflictWithoutCreatingUser() {
        IUserAdministrationMutationService userManagementService = Substitute.For<IUserAdministrationMutationService>();
        userManagementService
            .CreateAsync(Arg.Any<UserAdminCreateModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserAdminReadModel>(Errors.User.EmailAlreadyExists));
        var handler = new CreateAdminUserCommandHandler(
            userManagementService,
            Substitute.For<IEmailSender>(),
            Substitute.For<IAuditLogger>(),
            TimeProvider.System);

        Result<AdminUserCreationModel> result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(Errors.User.EmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WithEmptyActorId_ReturnsValidationFailure() {
        IUserAdministrationMutationService userManagementService = Substitute.For<IUserAdministrationMutationService>();
        var handler = new CreateAdminUserCommandHandler(
            userManagementService,
            Substitute.For<IEmailSender>(),
            Substitute.For<IAuditLogger>(),
            TimeProvider.System);

        Result<AdminUserCreationModel> result = await handler.Handle(
            CreateCommand() with { ActorUserId = Guid.Empty },
            CancellationToken.None);

        ResultAssert.Failure(result);
        await userManagementService.DidNotReceive().CreateAsync(Arg.Any<UserAdminCreateModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownRole_ReturnsValidationFailure() {
        IUserAdministrationMutationService userManagementService = Substitute.For<IUserAdministrationMutationService>();
        userManagementService
            .CreateAsync(Arg.Any<UserAdminCreateModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserAdminReadModel>(Errors.Validation.Invalid("Roles", "Unknown role.")));
        var handler = new CreateAdminUserCommandHandler(
            userManagementService,
            Substitute.For<IEmailSender>(),
            Substitute.For<IAuditLogger>(),
            TimeProvider.System);

        Result<AdminUserCreationModel> result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task Handle_WithExplicitPasswordAndNoEmail_CreatesUserWithoutPasswordChange() {
        IUserAdministrationMutationService userManagementService = Substitute.For<IUserAdministrationMutationService>();
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        userManagementService
            .CreateAsync(Arg.Any<UserAdminCreateModel>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(ToReadModel(call.Arg<UserAdminCreateModel>())));
        var handler = new CreateAdminUserCommandHandler(
            userManagementService,
            emailSender,
            Substitute.For<IAuditLogger>(),
            TimeProvider.System);

        Result<AdminUserCreationModel> result = await handler.Handle(
            CreateCommand() with {
                TemporaryPassword = " explicit-password ",
                GeneratePassword = false,
                SendCredentialsEmail = false,
                RequirePasswordChange = false,
            },
            CancellationToken.None);

        AdminUserCreationModel model = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal("explicit-password", model.TemporaryPassword),
            () => Assert.False(model.CredentialsEmailQueued));
        await emailSender.DidNotReceive().SendAccountCreatedAsync(
            Arg.Any<AccountCreatedMessage>(),
            Arg.Any<CancellationToken>());
    }

    private static UserAdminReadModel ToReadModel(UserAdminCreateModel request) {
        var user = User.Create(request.Email, "hash");
        user.UpdatePersonalInfo(request.FirstName, request.LastName);
        user.SetLanguage(request.Language ?? "en");
        user.SetEmailConfirmed(request.IsEmailConfirmed);
        user.ReplaceRoles([.. request.Roles.Select(Role.Create)]);
        if (request.RequirePasswordChange) {
            user.RequirePasswordChange();
        }

        return user.ToAdminReadModel();
    }

    private static CreateAdminUserCommand CreateCommand() =>
        new(
            Email: "dietologist@example.com",
            FirstName: "Test",
            LastName: "Dietologist",
            Language: "ru",
            Roles: [RoleNames.Dietologist],
            TemporaryPassword: null,
            GeneratePassword: true,
            IsEmailConfirmed: true,
            SendCredentialsEmail: true,
            RequirePasswordChange: true,
            ClientOrigin: "http://localhost:4200",
            ActorUserId: UserId.New().Value);
}
