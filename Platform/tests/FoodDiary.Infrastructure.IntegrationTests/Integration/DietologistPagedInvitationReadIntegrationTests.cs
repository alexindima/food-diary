using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Infrastructure.Persistence;
using FoodDiary.Modules.Dietologist.Infrastructure.Persistence.Recommendations;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition.Dietologist;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DietologistPagedInvitationReadIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ActivePages_FilterTheOwnerAndStatusAndOrderByAcceptanceThenIdentityWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var acceptedAt = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        using IDisposable creationTime = DomainTime.Override(new FixedTimeProvider(acceptedAt.AddDays(-2)));
        var coach = User.Create("paged-coach@example.test", "hash");
        var otherCoach = User.Create("other-coach@example.test", "hash");
        User[] clients = [.. Enumerable.Range(0, 6).Select(index => User.Create(FormattableString.Invariant($"paged-client-{index}@example.test"), "hash"))];
        context.Users.AddRange([coach, otherCoach, .. clients]);
        DietologistInvitation[] invitations = [.. clients.Select(client => DietologistInvitation.Create(client.Id, coach.Email!, "fixture-hash",
            acceptedAt.AddDays(7), new DietologistPermissions(ShareMeals: false, ShareWeight: false)))];
        for (int index = 0; index < invitations.Length; index++) {
            DietologistInvitation invitation = invitations[index];
            if (index != 3) {
                using IDisposable acceptanceTime = DomainTime.Override(new FixedTimeProvider(index == 0 ? acceptedAt.AddDays(-1) : acceptedAt));
                invitation.Accept(index == 4 ? otherCoach.Id : coach.Id);
            }
            if (index == 5) {
                using IDisposable revocationTime = DomainTime.Override(new FixedTimeProvider(acceptedAt.AddHours(1)));
                invitation.Revoke();
            }
            context.DietologistInvitations.Add(invitation);
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using var owned = new DietologistDbContext(new DbContextOptionsBuilder<DietologistDbContext>()
            .UseNpgsql(context.Database.GetConnectionString()).Options);
        var repository = new DietologistInvitationRepository(owned, new DietologistInvitationReadService(context));
        using var cancellation = new CancellationTokenSource();
        Guid[] newest = [.. invitations.Skip(1).Take(2).Select(invitation => invitation.Id.Value).Order()];

        IReadOnlyList<DietologistInvitationReadModel> first = await repository.GetActiveByDietologistPageReadModelsAsync(coach.Id, 1, 2, cancellation.Token);
        DietologistInvitationReadModel second = Assert.Single(await repository.GetActiveByDietologistPageReadModelsAsync(coach.Id, 2, 2, cancellation.Token));

        Assert.Equal(newest, first.Select(invitation => invitation.InvitationId));
        Assert.Equal(invitations[0].Id.Value, second.InvitationId);
        Assert.Equal(acceptedAt.AddDays(-1), second.AcceptedAtUtc);
        foreach (DietologistInvitationReadModel item in first) {
            User client = Assert.Single(clients, candidate => candidate.Id.Value == item.ClientUserId);
            Assert.Multiple(
                () => Assert.Equal(coach.Id.Value, item.DietologistUserId),
                () => Assert.Equal(coach.Email, item.DietologistUserEmail),
                () => Assert.Equal(client.Email, item.ClientEmail),
                () => Assert.Equal(DietologistInvitationStatus.Accepted, item.Status),
                () => Assert.Equal(acceptedAt, item.AcceptedAtUtc),
                () => Assert.False(item.Permissions.ShareMeals),
                () => Assert.False(item.Permissions.ShareWeight),
                () => Assert.True(item.Permissions.ShareStatistics));
        }
        Assert.Empty(await repository.GetActiveByDietologistPageReadModelsAsync(coach.Id, 3, 2, cancellation.Token));
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(owned.ChangeTracker.Entries());
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetActiveByDietologistPageReadModelsAsync(coach.Id, 1, 2, cancellation.Token));
    }

    [RequiresDockerFact]
    public async Task CommentParticipants_ReturnOnlyTheRequestedRecommendationAndHonorCancellationWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var coach = User.Create("participants-coach@example.test", "hash");
        var client = User.Create("participants-client@example.test", "hash");
        var recommendation = Recommendation.Create(coach.Id, client.Id, "Fixture recommendation");
        context.AddRange(coach, client, recommendation);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new RecommendationCommentRepository(context.RecommendationComments, new RecommendationCommentReadService(context));
        using var cancellation = new CancellationTokenSource();

        RecommendationParticipantsReadModel? participants = await repository.GetParticipantsAsync(recommendation.Id, cancellation.Token);

        Assert.Equal(new RecommendationParticipantsReadModel(client.Id, coach.Id), participants);
        Assert.Null(await repository.GetParticipantsAsync(RecommendationId.New(), cancellation.Token));
        Assert.Empty(context.ChangeTracker.Entries());
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetParticipantsAsync(recommendation.Id, cancellation.Token));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
