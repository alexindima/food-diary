using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Dietologist.Application.Services;
using FoodDiary.Modules.Dietologist.Contracts.Models;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistClientListBoundaryTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ClientLists_PropagateAccessFailuresBeforeReadingRelationships(bool paged, bool missingUser) {
        IDietologistInvitationReadModelRepository repository = Substitute.For<IDietologistInvitationReadModelRepository>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        var user = UserId.New();
        Error denied = missingUser ? new Error("Authentication.InvalidToken", "User is missing", ErrorKind.Unauthorized)
            : new Error("Users.Forbidden", "Not allowed", ErrorKind.Forbidden);
        using var cancellation = new CancellationTokenSource();
        UserId requested = missingUser ? UserId.Empty : user;
        access.EnsureCanAccessAsync(requested, cancellation.Token).Returns(denied);
        var service = new DietologistInvitationReadService(repository, access);

        Result<IReadOnlyList<ClientSummaryModel>> result = paged
            ? await service.GetMyClientsPageAsync(requested, 2, 7, cancellation.Token)
            : await service.GetMyClientsAsync(requested, cancellation.Token);

        Error error = ResultAssert.Failure(result);
        Assert.Empty(repository.ReceivedCalls());
        Assert.Equal(denied, error);
        await access.Received(1).EnsureCanAccessAsync(requested, cancellation.Token);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ClientLists_PreserveOrderAndMaskEveryPrivateProfileFieldUnlessShared(bool paged, bool empty) {
        IDietologistInvitationReadModelRepository repository = Substitute.For<IDietologistInvitationReadModelRepository>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        var user = UserId.New();
        using var cancellation = new CancellationTokenSource();
        DietologistInvitationReadModel[] rows = empty ? [] : [CreateInvitation(user, shareProfile: false), CreateInvitation(user, shareProfile: true)];
        repository.GetActiveByDietologistReadModelsAsync(user, cancellation.Token).Returns(rows);
        repository.GetActiveByDietologistPageReadModelsAsync(user, 2, 7, cancellation.Token).Returns(rows);
        var service = new DietologistInvitationReadService(repository, access);

        IReadOnlyList<ClientSummaryModel> result = ResultAssert.Success(paged
            ? await service.GetMyClientsPageAsync(user, 2, 7, cancellation.Token)
            : await service.GetMyClientsAsync(user, cancellation.Token));

        Assert.Equal(rows.Select(row => row.ClientUserId), result.Select(row => row.UserId));
        if (!empty) {
            for (int index = 0; index < 2; index++) {
                bool shared = index == 1;
                ClientSummaryModel client = result[index];
                Assert.Multiple(
                    () => Assert.Equal("client@example.test", client.Email),
                    () => Assert.Equal(shared ? "Anna" : null, client.FirstName),
                    () => Assert.Equal(shared ? "Example" : null, client.LastName),
                    () => Assert.Equal(shared ? "https://example.test/profile.jpg" : null, client.ProfileImage),
                    () => Assert.Equal(shared ? new DateTime(1992, 2, 29, 0, 0, 0, DateTimeKind.Utc) : (DateTime?)null, client.BirthDate),
                    () => Assert.Equal(shared ? "F" : null, client.Gender),
                    () => Assert.Equal(shared ? 171.5 : (double?)null, client.HeightCm),
                    () => Assert.Equal(shared ? "High" : null, client.ActivityLevel),
                    () => Assert.Equal(new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: false, ShareWeight: true,
                        ShareWaist: false, ShareGoals: true, ShareHydration: false, ShareProfile: shared, ShareFasting: true), client.Permissions),
                    () => Assert.Equal(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc), client.AcceptedAtUtc));
            }
        }
        if (paged) { await repository.Received(1).GetActiveByDietologistPageReadModelsAsync(user, 2, 7, cancellation.Token); } else {
            await repository.Received(1).GetActiveByDietologistReadModelsAsync(user, cancellation.Token);
        }
        Assert.Single(repository.ReceivedCalls());
        await access.Received(1).EnsureCanAccessAsync(user, cancellation.Token);
    }

    private static DietologistInvitationReadModel CreateInvitation(UserId dietologist, bool shareProfile) => new(
        Guid.NewGuid(), Guid.NewGuid(), dietologist.Value, "dietologist@example.test", "client@example.test", "Anna", "Example",
        "https://example.test/profile.jpg", new DateTime(1992, 2, 29, 0, 0, 0, DateTimeKind.Utc), "F", 171.5, ActivityLevel.High,
        "dietologist@example.test", "Doctor", "Example", DietologistInvitationStatus.Accepted,
        new DietologistPermissionsReadModel(ShareMeals: true, ShareStatistics: false, ShareWeight: true, ShareWaist: false,
            ShareGoals: true, ShareHydration: false, ShareProfile: shareProfile, ShareFasting: true),
        new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
}
