using FoodDiary.Testing;
using FoodDiary.Application.Users.Queries.CheckUserAccess;
using FoodDiary.Application.Users.Commands.RemoveUserPremiumRole;
using FoodDiary.Application.Users.Commands.EnsureUserPremiumRole;
using FoodDiary.Application.Users.Commands.StartUserPremiumTrial;
using FoodDiary.Application.Users.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Application.Users.Queries.GetUserBillingProfile;
using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Users.Commands.EnsureUserPremiumRole;
using FoodDiary.Application.Abstractions.Users.Commands.RemoveUserPremiumRole;
using FoodDiary.Application.Abstractions.Users.Commands.StartUserPremiumTrial;
using FoodDiary.Application.Abstractions.Users.Queries.CheckUserAccess;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfile;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UserBillingServiceTests {
    private static readonly DateTime Now = new(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    public async Task BillingProfileQuery_UsesPersistedAccessStateAndForwardsCancellation(bool active, bool deleted, bool accessible) {
        User trackedUser = CreateUser();
        IUserLookupRepository trackedReader = CreateReader(trackedUser);
        IUserBillingProfileReadModelRepository reader = Substitute.For<IUserBillingProfileReadModelRepository>();
        using var cancellation = new CancellationTokenSource();
        var model = new UserBillingProfileModel(trackedUser.Id, "persisted@example.com", active, deleted, HasPaidPremium: false, PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null);
        reader.GetBillingProfileIncludingDeletedAsync(trackedUser.Id, cancellation.Token).Returns(model);
        ISender sender = CreateService(trackedReader, billingProfileReader: reader);

        Result<UserBillingProfileModel> result = await sender.Send(new GetUserBillingProfileQuery(trackedUser.Id), cancellation.Token);

        if (accessible) {
            Assert.Same(model, ResultAssert.Success(result));
        } else {
            ResultAssert.Failure(result, Errors.Authentication.InvalidToken.Code);
        }
        await trackedReader.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await reader.Received(1).GetBillingProfileIncludingDeletedAsync(trackedUser.Id, cancellation.Token);
    }

    [Fact]
    public async Task GetAccessibleProfileAsync_MapsBillingOwnedView() {
        User user = CreateUser();
        user.ReplaceRoles([Role.Create(RoleNames.Premium)]);
        user.StartPremiumTrial(Now, TimeSpan.FromDays(7));
        IUserLookupRepository reader = CreateReader(user);
        ISender service = CreateService(reader);

        Result<UserBillingProfileModel> result = await service.Send(new GetUserBillingProfileQuery(UserId: user.Id), CancellationToken.None);

        UserBillingProfileModel profile = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(user.Id, profile.UserId),
            () => Assert.Equal(user.Email, profile.Email),
            () => Assert.True(profile.HasPaidPremium),
            () => Assert.Equal(Now, profile.PremiumTrialStartedAtUtc),
            () => Assert.Equal(Now.AddDays(7), profile.PremiumTrialEndsAtUtc));
    }

    [Fact]
    public async Task GetAccessibleProfileAsync_WhenUserDeleted_ReturnsInvalidToken() {
        User user = CreateUser();
        user.DeleteAccount(Now);
        ISender service = CreateService(CreateReader(user));

        Result<UserBillingProfileModel> result = await service.Send(new GetUserBillingProfileQuery(UserId: user.Id), CancellationToken.None);

        ResultAssert.Failure(result, Errors.Authentication.InvalidToken.Code);
    }

    [Fact]
    public async Task GetProfileIncludingDeletedAsync_ReturnsDeletedProjection() {
        User user = CreateUser();
        user.DeleteAccount(Now);
        IUserLookupRepository trackedReader = CreateReader(CreateUser());
        IUserBillingProfileReadModelRepository reader = Substitute.For<IUserBillingProfileReadModelRepository>();
        var expected = new UserBillingProfileModel(user.Id, user.Email, IsActive: false, IsDeleted: true,
            HasPaidPremium: false, PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null);
        reader.GetBillingProfileIncludingDeletedAsync(user.Id, CancellationToken.None).Returns(expected);
        ISender service = CreateService(trackedReader, billingProfileReader: reader);

        UserBillingProfileModel? result = await service.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: user.Id), CancellationToken.None);

        Assert.Same(expected, result);
        await trackedReader.DidNotReceiveWithAnyArgs().GetByIdIncludingDeletedAsync(default, default);
    }

    [Fact]
    public async Task StartPremiumTrialAsync_MutatesInsideUsersBoundaryAndPersists() {
        User user = CreateUser();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        ISender service = CreateService(CreateReader(user), writer);

        Result<UserBillingProfileModel> result = await service.Send(new StartUserPremiumTrialCommand(UserId: user.Id, StartedAtUtc: Now, Duration: TimeSpan.FromDays(7)), CancellationToken.None);

        UserBillingProfileModel profile = ResultAssert.Success(result);
        Assert.Equal(Now.AddDays(7), profile.PremiumTrialEndsAtUtc);
        await writer.Received(1).UpdateAsync(user, CancellationToken.None);
    }

    [Fact]
    public async Task StartPremiumTrialAsync_WhenUserIsMissing_ReturnsAccessFailureWithoutWriting() {
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        ISender service = CreateService(Substitute.For<IUserLookupRepository>(), writer);

        Result<UserBillingProfileModel> result = await service.Send(new StartUserPremiumTrialCommand(UserId: UserId.New(), StartedAtUtc: Now, Duration: TimeSpan.FromDays(7)), CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
        await writer.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task EnsureCanAccessAsync_ReturnsPolicyResult() {
        User user = CreateUser();
        ISender accessibleService = CreateService(CreateReader(user));
        ISender missingService = CreateService(Substitute.For<IUserLookupRepository>());

        Error? accessible = await accessibleService.Send(new CheckUserAccessQuery(UserId: user.Id), CancellationToken.None);
        Error? missing = await missingService.Send(new CheckUserAccessQuery(UserId: UserId.New()), CancellationToken.None);

        Assert.Multiple(
            () => Assert.Null(accessible),
            () => Assert.Equal("Authentication.InvalidToken", missing?.Code));
    }

    [Fact]
    public async Task PremiumRoleMethods_DelegateByUserId() {
        IUserRoleMembershipService roles = Substitute.For<IUserRoleMembershipService>();
        ISender service = CreateService(Substitute.For<IUserLookupRepository>(), roleMembershipService: roles);
        var userId = UserId.New();

        await service.Send(new EnsureUserPremiumRoleCommand(UserId: userId), CancellationToken.None);
        await service.Send(new RemoveUserPremiumRoleCommand(UserId: userId), CancellationToken.None);

        await roles.Received(1).EnsureRoleAsync(userId, RoleNames.Premium, CancellationToken.None);
        await roles.Received(1).RemoveRoleAsync(userId, RoleNames.Premium, CancellationToken.None);
    }

    private static ISender CreateService(
        IUserLookupRepository reader,
        IUserWriteRepository? writer = null,
        IUserRoleMembershipService? roleMembershipService = null,
        IUserBillingProfileReadModelRepository? billingProfileReader = null) =>
        RequestTestSender.Create(
            new GetUserBillingProfileQueryHandler(billingProfileReader ?? reader as IUserBillingProfileReadModelRepository ?? Substitute.For<IUserBillingProfileReadModelRepository>()),
            new GetUserBillingProfileIncludingDeletedQueryHandler(billingProfileReader ?? Substitute.For<IUserBillingProfileReadModelRepository>()),
            new StartUserPremiumTrialCommandHandler(reader, writer ?? Substitute.For<IUserWriteRepository>()),
            new EnsureUserPremiumRoleCommandHandler(roleMembershipService ?? Substitute.For<IUserRoleMembershipService>()),
            new RemoveUserPremiumRoleCommandHandler(roleMembershipService ?? Substitute.For<IUserRoleMembershipService>()),
            new CheckUserAccessQueryHandler(reader as ICurrentUserAccessService ?? MissingAccessService()));

    private static IUserLookupRepository CreateReader(User user) {
        IUserLookupRepository reader = Substitute.For<IUserLookupRepository, IUserBillingProfileReadModelRepository, ICurrentUserAccessService>();
        reader.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        reader.GetByIdIncludingDeletedAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        ((IUserBillingProfileReadModelRepository)reader).GetBillingProfileIncludingDeletedAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new UserBillingProfileModel(user.Id, user.Email, user.IsActive, user.DeletedAt is not null,
                user.HasRole(RoleNames.Premium), user.PremiumTrialStartedAtUtc, user.PremiumTrialEndsAtUtc, user.IsEmailConfirmed));
        ((ICurrentUserAccessService)reader).EnsureCanAccessAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user.IsActive && user.DeletedAt is null ? null : Errors.Authentication.InvalidToken);
        return reader;
    }

    private static ICurrentUserAccessService MissingAccessService() {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(Errors.Authentication.InvalidToken);
        return service;
    }

    private static User CreateUser() => User.Create("billing-boundary@example.com", "hash");
}
