using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication.Services;

[ExcludeFromCodeCoverage]
public sealed class InitialAdminBootstrapServiceTests {
    private readonly IUserAuthenticationRegistrationService _userRegistrationService =
        Substitute.For<IUserAuthenticationRegistrationService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task BootstrapAsync_WhenPasswordIsBlank_SkipsDatabaseWork() {
        InitialAdminBootstrapService service = CreateService();

        Result<BootstrapInitialAdminModel> result =
            await service.BootstrapAsync(" owner@fooddiary.test ", " ", CancellationToken.None);

        BootstrapInitialAdminModel model = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(BootstrapInitialAdminStatus.SkippedMissingPassword, model.Status),
            () => Assert.Equal("owner@fooddiary.test", model.Email));
        await _userRegistrationService
            .DidNotReceiveWithAnyArgs()
            .BootstrapInitialAdminAsync(default!, default!, default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task BootstrapAsync_WhenUserExists_DoesNotCreateDuplicate() {
        _userRegistrationService
            .BootstrapInitialAdminAsync(
                "owner@fooddiary.test",
                "StrongPassword123",
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new UserInitialAdminBootstrapModel(Created: false, "owner@fooddiary.test"));
        InitialAdminBootstrapService service = CreateService();

        Result<BootstrapInitialAdminModel> result =
            await service.BootstrapAsync(
                "owner@fooddiary.test",
                "StrongPassword123",
                CancellationToken.None);

        BootstrapInitialAdminModel model = ResultAssert.Success(result);
        Assert.Equal(BootstrapInitialAdminStatus.SkippedExistingUser, model.Status);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task BootstrapAsync_WhenConfigured_CreatesAndPersistsConfirmedAdmin() {
        _userRegistrationService
            .BootstrapInitialAdminAsync(
                "owner@fooddiary.test",
                "StrongPassword123",
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new UserInitialAdminBootstrapModel(Created: true, "owner@fooddiary.test"));
        InitialAdminBootstrapService service = CreateService();

        Result<BootstrapInitialAdminModel> result =
            await service.BootstrapAsync(
                " owner@fooddiary.test ",
                "StrongPassword123",
                CancellationToken.None);

        BootstrapInitialAdminModel model = ResultAssert.Success(result);
        Assert.Equal(BootstrapInitialAdminStatus.Created, model.Status);
        await _userRegistrationService.Received(1).BootstrapInitialAdminAsync(
            "owner@fooddiary.test",
            "StrongPassword123",
            Arg.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(
                new[] { RoleNames.Owner, RoleNames.Admin, RoleNames.Premium },
                StringComparer.Ordinal)),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private InitialAdminBootstrapService CreateService() =>
        new(_userRegistrationService, _unitOfWork);
}
