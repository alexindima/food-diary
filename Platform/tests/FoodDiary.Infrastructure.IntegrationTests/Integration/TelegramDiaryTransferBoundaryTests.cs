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
