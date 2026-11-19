using FoodDiary.Application.Runtime;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelegramDiaryTransferBoundaryTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task TransferRequiresACallerTransactionBeforeReadingOrChangingAccounts() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        IUserSessionRevocationService sessions = Substitute.For<IUserSessionRevocationService>();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, sessions);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IUserTelegramDiaryTransfer transfer = scope.ServiceProvider.GetRequiredService<IUserTelegramDiaryTransfer>();
        var source = User.CreateTelegram(456, "hash");
        var target = User.Create("target@example.test", "hash");
        var proof = new TelegramDiaryProof(source.Id, source.SecurityVersion, target.Id, target.SecurityVersion, target.Email!, AdminTargetConfirmed: false);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => transfer.TransferAsync(proof, useTelegramGoals: false, CancellationToken.None));

        Assert.Equal("Diary transfer requires a caller transaction.", error.Message);
        Assert.Empty(sessions.ReceivedCalls());
        Assert.Empty(await database.Users.ToListAsync());
    }

    [RequiresDockerFact]
    public async Task TransferMovesTheOidcBindingAndRevokesBothOwnersSessionsAtomically() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var source = User.CreateTelegram(456, "hash");
        const string issuer = "https://oauth.telegram.org";
        const string subject = "456";
        source.BindTelegramOidcIdentity(issuer, subject);
        var target = User.Create("oidc-target@example.test", "hash");
        target.SetEmailConfirmed(isConfirmed: true);
        database.Users.AddRange(source, target);
        await database.SaveChangesAsync();
        var proof = new TelegramDiaryProof(source.Id, source.SecurityVersion, target.Id, target.SecurityVersion, target.Email!, AdminTargetConfirmed: false);
        IUserSessionRevocationService sessions = Substitute.For<IUserSessionRevocationService>();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, sessions);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IModuleTransactionCoordinator transactions = scope.ServiceProvider.GetRequiredService<IModuleTransactionCoordinator>();
        IUserTelegramDiaryTransfer transfer = scope.ServiceProvider.GetRequiredService<IUserTelegramDiaryTransfer>();

        ResultAssert.Success(await transactions.ExecuteAsync((_, token) => transfer.TransferAsync(proof, useTelegramGoals: false, token), CancellationToken.None));

        User persistedSource = await database.Users.AsNoTracking().SingleAsync(user => user.Id == source.Id);
        User persistedTarget = await database.Users.AsNoTracking().SingleAsync(user => user.Id == target.Id);
        Assert.NotNull(persistedSource.DeletedAt);
        Assert.Null(persistedSource.TelegramUserId);
        Assert.Equal(456, persistedTarget.TelegramUserId);
        Assert.Equal(issuer, persistedTarget.TelegramOidcIssuer);
        Assert.Equal(subject, persistedTarget.TelegramOidcSubject);
        await sessions.Received(1).RevokeAllAsync(source.Id, Arg.Any<DateTime>(), CancellationToken.None);
        await sessions.Received(1).RevokeAllAsync(target.Id, Arg.Any<DateTime>(), CancellationToken.None);
    }

    [RequiresDockerTheory]
    [InlineData("changed_version", "Authentication.TelegramDiaryProofExpired")]
    [InlineData("privileged_target", "User.TelegramDiaryRestrictedAccount")]
    [InlineData("source_has_email", "User.TelegramDiaryTransferUnavailable")]
    [InlineData("source_tracks_weight", "User.TelegramDiaryTransferUnavailable")]
    [InlineData("source_has_billing", "User.TelegramDiaryTransferUnavailable")]
    [InlineData("duplicate_measurement_day", "User.TelegramDiaryMeasurementConflict")]
    public async Task Transfer_LeavesBothOwnersUntouchedWhenTheCurrentDiariesCannotBeCombined(string conflict, string expectedCode) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var source = User.CreateTelegram(456, "hash");
        var target = User.Create("target@example.test", "hash");
        target.SetEmailConfirmed(isConfirmed: true);
        DateTime now = DateTime.UtcNow;
        switch (conflict) {
            case "privileged_target":
                Role support = await database.Roles.SingleAsync(role => role.Name == RoleNames.Support);
                target.ReplaceRoles([support]);
                break;
            case "source_has_email":
                source.AddVerifiedEmail("source@example.test");
                break;
            case "source_tracks_weight":
                source.StartWeightGoal(70, 80, now);
                break;
            case "source_has_billing":
                database.Add(BillingSubscription.CreatePending(source.Id, BillingProviderNames.Paddle, "fixture-customer", "fixture-price", "monthly"));
                break;
            case "duplicate_measurement_day":
                database.AddRange(WeightEntry.Create(source.Id, now.Date, 79), WeightEntry.Create(target.Id, now.Date, 81));
                break;
        }
        database.Users.AddRange(source, target);
        await database.SaveChangesAsync();
        var proof = new TelegramDiaryProof(source.Id, source.SecurityVersion, target.Id,
            conflict is "changed_version" ? target.SecurityVersion + 1 : target.SecurityVersion,
            "target@example.test", AdminTargetConfirmed: false);
        IUserSessionRevocationService sessions = Substitute.For<IUserSessionRevocationService>();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, sessions);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IModuleTransactionCoordinator transactions = scope.ServiceProvider.GetRequiredService<IModuleTransactionCoordinator>();
        IUserTelegramDiaryTransfer transfer = scope.ServiceProvider.GetRequiredService<IUserTelegramDiaryTransfer>();

        Result<TelegramDiaryPreview> result = await transactions.ExecuteAsync(
            (_, token) => transfer.TransferAsync(proof, useTelegramGoals: false, token), CancellationToken.None);

        Assert.Equal(expectedCode, ResultAssert.Failure(result).Code);
        Assert.Empty(sessions.ReceivedCalls());
        var reloadedSource = await database.Users.Where(user => user.Id == source.Id)
            .Select(user => new { user.TelegramUserId, user.DeletedAt }).SingleAsync();
        var reloadedTarget = await database.Users.Where(user => user.Id == target.Id)
            .Select(user => new { user.TelegramUserId, user.SecurityVersion }).SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(source.TelegramUserId, reloadedSource.TelegramUserId),
            () => Assert.Null(reloadedSource.DeletedAt),
            () => Assert.Null(reloadedTarget.TelegramUserId),
            () => Assert.Equal(target.SecurityVersion, reloadedTarget.SecurityVersion));
    }

    private static ServiceProvider CreateProvider(string connectionString, IUserSessionRevocationService sessions) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Database:EnableRetries"] = "false",
            }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddApplicationRuntime();
        services.AddUsersPersistence();
        services.AddReadModelComposition();
        services.AddSingleton(sessions);
        return services.BuildServiceProvider();
    }
}
