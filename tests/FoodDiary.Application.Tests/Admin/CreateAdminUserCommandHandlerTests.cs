using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Commands.CreateAdminUser;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Admin;

public sealed class CreateAdminUserCommandHandlerTests {
    [Fact]
    public async Task Handle_WithGeneratedPassword_CreatesConfirmedUserAndQueuesCredentialsEmail() {
        IAdminUserManagementService userManagementService = Substitute.For<IAdminUserManagementService>();
        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        IAuditLogger auditLogger = Substitute.For<IAuditLogger>();
        var role = Role.Create(RoleNames.Dietologist);
        User? createdUser = null;

        userManagementService
            .GetByEmailIncludingDeletedAsync("dietologist@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        userManagementService
            .GetRolesByNamesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns([role]);
        userManagementService
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => {
                createdUser = callInfo.Arg<User>();
                return createdUser!;
            });
        passwordHasher.Hash(Arg.Any<string>()).Returns(callInfo => $"hashed:{callInfo.Arg<string>()}");

        var handler = new CreateAdminUserCommandHandler(
            userManagementService,
            passwordHasher,
            emailSender,
            auditLogger,
            TimeProvider.System);

        Result<AdminUserCreationModel> result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        ResultAssert.Success(result);
        User capturedUser = Assert.IsType<User>(createdUser);
        Assert.True(capturedUser.IsEmailConfirmed);
        Assert.True(capturedUser.MustChangePassword);
        Assert.Contains(RoleNames.Dietologist, capturedUser.GetRoleNames());
        AdminUserCreationModel creation = result.Value;
        Assert.NotEmpty(creation.TemporaryPassword);
        Assert.True(creation.CredentialsEmailQueued);
        await emailSender.Received(1).SendAccountCreatedAsync(
            Arg.Is<AccountCreatedMessage>((AccountCreatedMessage? message) =>
                message != null &&
                string.Equals(message.ToEmail, "dietologist@example.com", StringComparison.Ordinal) &&
                string.Equals(message.TemporaryPassword, creation.TemporaryPassword, StringComparison.Ordinal) &&
                string.Equals(message.Language, "ru", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        await userManagementService.Received(1).UpdateAsync(
            capturedUser,
            Arg.Is<IReadOnlyCollection<UserRoleAuditEvent>>(events =>
                events != null &&
                events.Count == 1 &&
                string.Equals(events.Single().RoleName, RoleNames.Dietologist, StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsConflictWithoutCreatingUser() {
        IAdminUserManagementService userManagementService = Substitute.For<IAdminUserManagementService>();
        userManagementService
            .GetByEmailIncludingDeletedAsync("dietologist@example.com", Arg.Any<CancellationToken>())
            .Returns(User.Create("dietologist@example.com", "hash"));
        var handler = new CreateAdminUserCommandHandler(
            userManagementService,
            Substitute.For<IPasswordHasher>(),
            Substitute.For<IEmailSender>(),
            Substitute.For<IAuditLogger>(),
            TimeProvider.System);

        Result<AdminUserCreationModel> result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(Errors.User.EmailAlreadyExists, result.Error);
        await userManagementService.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
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
