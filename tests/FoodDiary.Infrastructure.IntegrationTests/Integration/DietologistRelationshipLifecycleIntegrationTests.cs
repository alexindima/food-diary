using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DietologistRelationshipLifecycleIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task UserDeletion_PreservesTaskRestrictionsInvitationSetNullAndRecommendationCascade() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        string dietologistEmail = $"dietologist-lifecycle-{Guid.NewGuid():N}@example.com";
        var dietologist = User.Create(dietologistEmail, "hash");
        var client = User.Create($"client-lifecycle-{Guid.NewGuid():N}@example.com", "hash");
        var invitation = DietologistInvitation.Create(
            client.Id, dietologistEmail, "test-token-hash", DateTime.UtcNow.AddDays(1),
            DietologistPermissions.AllEnabled);
        invitation.Accept(dietologist.Id);
        var task = ClientTask.Create(dietologist.Id, client.Id, "Test task", details: null, dueAtUtc: null);
        var recommendation = Recommendation.Create(dietologist.Id, client.Id, "Test recommendation");
        context.Users.AddRange(dietologist, client);
        context.Set<DietologistInvitation>().Add(invitation);
        context.Set<ClientTask>().Add(task);
        context.Set<Recommendation>().Add(recommendation);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        foreach (UserId userId in new[] { dietologist.Id, client.Id }) {
            PostgresException exception = await Assert.ThrowsAsync<PostgresException>(() =>
                context.Users.Where(user => user.Id == userId).ExecuteDeleteAsync());
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
            Assert.True(await context.Users.AnyAsync(user => user.Id == userId));
        }
        Assert.True(await context.Set<ClientTask>().AnyAsync(item => item.Id == task.Id));
        Assert.Equal(dietologist.Id, await context.Set<DietologistInvitation>()
            .Where(item => item.Id == invitation.Id).Select(item => item.DietologistUserId).SingleAsync());

        await context.Set<ClientTask>().Where(item => item.Id == task.Id).ExecuteDeleteAsync();
        await context.Users.Where(user => user.Id == dietologist.Id).ExecuteDeleteAsync();

        DietologistInvitation retainedInvitation = await context.Set<DietologistInvitation>()
            .AsNoTracking().SingleAsync(item => item.Id == invitation.Id);
        Assert.Null(retainedInvitation.DietologistUserId);
        Assert.Equal(client.Id, retainedInvitation.ClientUserId);
        Assert.False(await context.Set<Recommendation>().AnyAsync(item => item.Id == recommendation.Id));

        await context.Users.Where(user => user.Id == client.Id).ExecuteDeleteAsync();
        Assert.False(await context.Set<DietologistInvitation>().AnyAsync(item => item.Id == invitation.Id));
    }
}
