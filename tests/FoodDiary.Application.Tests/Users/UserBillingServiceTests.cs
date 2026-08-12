using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UserBillingServiceTests {
    private static readonly DateTime Now = new(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetAccessibleProfileAsync_MapsBillingOwnedView() {
        User user = CreateUser();
        user.ReplaceRoles([Role.Create(RoleNames.Premium)]);
        user.StartPremiumTrial(Now, TimeSpan.FromDays(7));
        IUserLookupRepository reader = CreateReader(user);
        UserBillingService service = CreateService(reader);

        Result<UserBillingProfileModel> result = await service.GetAccessibleProfileAsync(user.Id, CancellationToken.None);

        UserBillingProfileModel profile = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(user.Id, profile.UserId),
            () => Assert.Equal(user.Email, profile.Email),
            () => Assert.True(profile.HasPaidPremium),
            () => Assert.Equal(Now, profile.PremiumTrialStartedAtUtc),
            () => Assert.Equal(Now.AddDays(7), profile.PremiumTrialEndsAtUtc));
    }

    [Fact]
    public async Task GetAccessibleProfileAsync_WhenUserDeleted_ReturnsAccountDeleted() {
        User user = CreateUser();
        user.DeleteAccount(Now);
        UserBillingService service = CreateService(CreateReader(user));

        Result<UserBillingProfileModel> result = await service.GetAccessibleProfileAsync(user.Id, CancellationToken.None);

        ResultAssert.Failure(result, Errors.Authentication.AccountDeleted.Code);
    }

    [Fact]
    public async Task GetProfileIncludingDeletedAsync_ReturnsDeletedProjection() {
        User user = CreateUser();
        user.DeleteAccount(Now);
        UserBillingService service = CreateService(CreateReader(user));

        UserBillingProfileModel? result = await service.GetProfileIncludingDeletedAsync(user.Id, CancellationToken.None);

        Assert.True(result?.IsDeleted);
    }

    [Fact]
    public async Task StartPremiumTrialAsync_MutatesInsideUsersBoundaryAndPersists() {
        User user = CreateUser();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        UserBillingService service = CreateService(CreateReader(user), writer);

        Result<UserBillingProfileModel> result = await service.StartPremiumTrialAsync(
            user.Id,
            Now,
            TimeSpan.FromDays(7),
            CancellationToken.None);

        UserBillingProfileModel profile = ResultAssert.Success(result);
        Assert.Equal(Now.AddDays(7), profile.PremiumTrialEndsAtUtc);
        await writer.Received(1).UpdateAsync(user, CancellationToken.None);
    }

    [Fact]
    public async Task PremiumRoleMethods_DelegateByUserId() {
        IUserRoleMembershipService roles = Substitute.For<IUserRoleMembershipService>();
        UserBillingService service = CreateService(Substitute.For<IUserLookupRepository>(), roleMembershipService: roles);
        var userId = UserId.New();

        await service.EnsurePremiumRoleAsync(userId, CancellationToken.None);
        await service.RemovePremiumRoleAsync(userId, CancellationToken.None);

        await roles.Received(1).EnsureRoleAsync(userId, RoleNames.Premium, CancellationToken.None);
        await roles.Received(1).RemoveRoleAsync(userId, RoleNames.Premium, CancellationToken.None);
    }

    private static UserBillingService CreateService(
        IUserLookupRepository reader,
        IUserWriteRepository? writer = null,
        IUserRoleMembershipService? roleMembershipService = null) =>
        new(
            reader,
            writer ?? Substitute.For<IUserWriteRepository>(),
            roleMembershipService ?? Substitute.For<IUserRoleMembershipService>());

    private static IUserLookupRepository CreateReader(User user) {
        IUserLookupRepository reader = Substitute.For<IUserLookupRepository>();
        reader.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        reader.GetByIdIncludingDeletedAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return reader;
    }

    private static User CreateUser() => User.Create("billing-boundary@example.com", "hash");
}
