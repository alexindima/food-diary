using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Identity.Domain.Entities.Content;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Admin;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class PagedIdentityProjectionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task EmailTemplatePages_OrderByKeyThenLocaleAndProjectWithoutTrackingAfterSynchronization() {
        await using IdentityDbContext context = await IdentityContextTestFactory.CreateAsync(databaseFixture);
        await context.EmailTemplates.ExecuteDeleteAsync();
        EmailTemplate[] templates = [
            EmailTemplate.Create("a", "ru", "A ru", "<p>A ru</p>", "A ru", isActive: true),
            EmailTemplate.Create("c", "en", "C", "<p>C</p>", "C", isActive: true),
            EmailTemplate.Create("b", "en", "B", "<p>B</p>", "B", isActive: true),
            EmailTemplate.Create("a", "en", "A en", "<p>A en</p>", "A en", isActive: true),
            EmailTemplate.Create("d", "en", "D", "<p>D</p>", "D", isActive: true),
        ];
        templates[2].Update("Updated B", "<p>Updated B</p>", "Updated B text", isActive: false);
        context.EmailTemplates.AddRange(templates);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        using var cancellation = new CancellationTokenSource();
        List<CancellationToken> synchronizations = [];
        var synchronized = new EmailTemplateRepository(context.EmailTemplates, token => { synchronizations.Add(token); return Task.CompletedTask; });
        var standalone = new EmailTemplateRepository(context.EmailTemplates);

        IReadOnlyList<EmailTemplateReadModel> first = await standalone.GetPageReadModelsAsync(1, 2, cancellation.Token);
        IReadOnlyList<EmailTemplateReadModel> second = await synchronized.GetPageReadModelsAsync(2, 2, cancellation.Token);
        IReadOnlyList<EmailTemplateReadModel> empty = await synchronized.GetPageReadModelsAsync(4, 2, cancellation.Token);

        Assert.Equal(new[] { "a:en", "a:ru" }, first.Select(item => item.Key + ":" + item.Locale), StringComparer.Ordinal);
        Assert.Equal(new[] { "b", "c" }, second.Select(item => item.Key), StringComparer.Ordinal);
        EmailTemplateReadModel updated = second[0];
        Assert.Multiple(
            () => Assert.Equal(templates[2].Id, updated.Id),
            () => Assert.Equal("en", updated.Locale),
            () => Assert.Equal("Updated B", updated.Subject),
            () => Assert.Equal("<p>Updated B</p>", updated.HtmlBody),
            () => Assert.Equal("Updated B text", updated.TextBody),
            () => Assert.False(updated.IsActive),
            () => Assert.Equal(DateTimeKind.Utc, updated.CreatedOnUtc.Kind),
            () => Assert.NotNull(updated.ModifiedOnUtc),
            () => Assert.Empty(empty),
            () => Assert.Equal(new[] { cancellation.Token, cancellation.Token }, synchronizations),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task ActiveSessionPages_ExcludeRevokedAndForeignSessionsAndUseStableIdentityOrderingForTies() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create("paged-owner@example.test", "hash");
        var foreign = User.Create("paged-foreign@example.test", "hash");
        database.Users.AddRange(owner, foreign);
        await database.SaveChangesAsync();
        await using IdentityDbContext context = IdentityContextTestFactory.Create(database.Database.GetConnectionString()!);
        var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        UserRefreshTokenSession latest = CreateSession("00000000-0000-0000-0000-000000000003", owner.Id, now.AddMinutes(2), "latest");
        UserRefreshTokenSession firstTie = CreateSession("00000000-0000-0000-0000-000000000001", owner.Id, now.AddMinutes(1), "tie-first");
        UserRefreshTokenSession secondTie = CreateSession("00000000-0000-0000-0000-000000000002", owner.Id, now.AddMinutes(1), "tie-second");
        UserRefreshTokenSession revoked = CreateSession("00000000-0000-0000-0000-000000000004", owner.Id, now.AddMinutes(4), "revoked");
        revoked.Revoke(now.AddMinutes(5));
        UserRefreshTokenSession foreignSession = CreateSession("00000000-0000-0000-0000-000000000005", foreign.Id, now.AddMinutes(5), "foreign");
        context.UserRefreshTokenSessions.AddRange(secondTie, revoked, latest, foreignSession, firstTie);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        using var cancellation = new CancellationTokenSource();
        List<CancellationToken> synchronizations = [];
        var synchronized = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database,
            token => { synchronizations.Add(token); return Task.CompletedTask; });
        var standalone = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database);

        IReadOnlyList<RefreshTokenSessionReadModel> first = await standalone.GetActivePageReadModelsAsync(owner.Id, 1, 1, cancellation.Token);
        IReadOnlyList<RefreshTokenSessionReadModel> second = await synchronized.GetActivePageReadModelsAsync(owner.Id, 2, 1, cancellation.Token);
        IReadOnlyList<RefreshTokenSessionReadModel> third = await synchronized.GetActivePageReadModelsAsync(owner.Id, 3, 1, cancellation.Token);
        IReadOnlyList<RefreshTokenSessionReadModel> empty = await synchronized.GetActivePageReadModelsAsync(owner.Id, 4, 1, cancellation.Token);

        Assert.Equal(latest.Id, Assert.Single(first).Id);
        RefreshTokenSessionReadModel row = Assert.Single(second);
        Assert.Equal(secondTie.Id, Assert.Single(third).Id);
        Assert.Multiple(
            () => Assert.Equal(firstTie.Id, row.Id),
            () => Assert.Equal("password", row.AuthProvider),
            () => Assert.Equal("tie-first", row.UserAgent),
            () => Assert.Equal(now.AddMinutes(1), row.CreatedAtUtc),
            () => Assert.Equal(now.AddMinutes(1), row.LastRotatedAtUtc),
            () => Assert.Empty(empty),
            () => Assert.Equal(new[] { cancellation.Token, cancellation.Token, cancellation.Token }, synchronizations),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    private static UserRefreshTokenSession CreateSession(string id, UserId owner, DateTime createdAt, string agent) =>
        UserRefreshTokenSession.Create(new RefreshTokenSessionId(Guid.Parse(id)), owner, "hash-" + agent, rememberMe: false,
            authProvider: "password", ipAddress: null, userAgent: agent, nowUtc: createdAt);
}
