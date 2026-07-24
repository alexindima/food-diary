using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication.Services;

[ExcludeFromCodeCoverage]
public sealed class InitialAdminBootstrapServiceTests {
    private readonly IAuthenticationUserRegistrationService userRegistrationService =
        Substitute.For<IAuthenticationUserRegistrationService>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task BootstrapAsync_WhenPasswordIsBlank_SkipsDatabaseWork() {
        InitialAdminBootstrapService service = CreateService();

        Result<BootstrapInitialAdminModel> result =
            await service.BootstrapAsync(" owner@fooddiary.test ", " ", CancellationToken.None);

        BootstrapInitialAdminModel model = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(BootstrapInitialAdminStatus.SkippedMissingPassword, model.Status),
            () => Assert.Equal("owner@fooddiary.test", model.Email));
        await userRegistrationService
            .DidNotReceiveWithAnyArgs()
            .GetByEmailIncludingDeletedAsync(default!, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task BootstrapAsync_WhenUserExists_DoesNotCreateDuplicate() {
        var existingUser = User.Create("owner@fooddiary.test", "existing-hash");
        userRegistrationService
            .GetByEmailIncludingDeletedAsync("owner@fooddiary.test", Arg.Any<CancellationToken>())
            .Returns(existingUser);
        InitialAdminBootstrapService service = CreateService();

        Result<BootstrapInitialAdminModel> result =
            await service.BootstrapAsync(
                "owner@fooddiary.test",
                "StrongPassword123",
                CancellationToken.None);

        BootstrapInitialAdminModel model = ResultAssert.Success(result);
        Assert.Equal(BootstrapInitialAdminStatus.SkippedExistingUser, model.Status);
        await userRegistrationService.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task BootstrapAsync_WhenConfigured_CreatesAndPersistsConfirmedAdmin() {
        Role[] roles = [
            Role.Create(RoleNames.Owner),
            Role.Create(RoleNames.Admin),
            Role.Create(RoleNames.Premium),
        ];
        userRegistrationService
            .EnsureRolesByNamesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(roles);
        passwordHasher.Hash("StrongPassword123").Returns("test-hash");
        User? capturedUser = null;
        _ = userRegistrationService
            .AddAsync(Arg.Do<User>(user => capturedUser = user), Arg.Any<CancellationToken>());
        InitialAdminBootstrapService service = CreateService();

        Result<BootstrapInitialAdminModel> result =
            await service.BootstrapAsync(
                " owner@fooddiary.test ",
                "StrongPassword123",
                CancellationToken.None);

        BootstrapInitialAdminModel model = ResultAssert.Success(result);
        User admin = Assert.IsType<User>(capturedUser);
        Assert.Multiple(
            () => Assert.Equal(BootstrapInitialAdminStatus.Created, model.Status),
            () => Assert.Equal("owner@fooddiary.test", admin.Email),
            () => Assert.Equal("test-hash", admin.Password),
            () => Assert.True(admin.IsEmailConfirmed),
            () => Assert.Equal([RoleNames.Owner, RoleNames.Admin, RoleNames.Premium], admin.GetRoleNames()));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private InitialAdminBootstrapService CreateService() =>
        new(userRegistrationService, passwordHasher, unitOfWork);
}
