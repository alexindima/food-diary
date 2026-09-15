using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Modules.Hydration.Application.Commands.CreateHydrationFromOperation;
using FoodDiary.Modules.Hydration.Application.Models;
using FoodDiary.Modules.Hydration.Infrastructure;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Application.Commands.ConfirmUpload;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SerializedOwnerCommandsIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(250)]
    [InlineData(500)]
    public async Task Hydration_ConcurrentOperationReturnsSavedResultOrInputConflict(int secondAmount) {
        await using FoodDiaryDbContext firstContext = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"serialized-water-{Guid.NewGuid():N}@example.com", "hash");
        firstContext.Users.Add(user);
        await firstContext.SaveChangesAsync();
        firstContext.ChangeTracker.Clear();
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(firstContext.Database.GetConnectionString()!);
        await using ServiceProvider firstProvider = CreateProvider(firstContext);
        await using ServiceProvider secondProvider = CreateProvider(secondContext);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IHydrationOperationReceiptRepository actual = firstProvider.GetRequiredService<IHydrationOperationReceiptRepository>();
        IHydrationOperationReceiptRepository paused = Substitute.For<IHydrationOperationReceiptRepository>();
        var operationId = Guid.NewGuid();
        paused.FindAsync(user.Id, operationId, Arg.Any<CancellationToken>()).Returns(async call => {
            entered.TrySetResult();
            await release.Task.WaitAsync(timeout.Token);
            return await actual.FindAsync(user.Id, operationId, call.Arg<CancellationToken>());
        });
        paused.AddAsync(Arg.Any<FoodDiary.Modules.Hydration.Domain.Entities.Tracking.HydrationOperationReceipt>(), Arg.Any<CancellationToken>())
            .Returns(call => actual.AddAsync(call.Arg<FoodDiary.Modules.Hydration.Domain.Entities.Tracking.HydrationOperationReceipt>(), call.Arg<CancellationToken>()));
        var firstHandler = new CreateHydrationFromOperationCommandHandler(
            firstProvider.GetRequiredService<IHydrationEntryWriteRepository>(), paused,
            Substitute.For<ICurrentUserAccessService>(), firstProvider.GetRequiredService<IHydrationOperationTransactionRunner>());
        var secondHandler = new CreateHydrationFromOperationCommandHandler(
            secondProvider.GetRequiredService<IHydrationEntryWriteRepository>(), secondProvider.GetRequiredService<IHydrationOperationReceiptRepository>(),
            Substitute.For<ICurrentUserAccessService>(), secondProvider.GetRequiredService<IHydrationOperationTransactionRunner>());
        var command = new CreateHydrationFromOperationCommand(user.Id.Value, operationId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 250);
        Task<Result<HydrationOperationModel>> first = firstHandler.Handle(command, timeout.Token);
        await entered.Task.WaitAsync(timeout.Token);
        Task<Result<HydrationOperationModel>> second = secondHandler.Handle(command with { AmountMl = secondAmount }, timeout.Token);
        release.TrySetResult();
        Result<HydrationOperationModel>[] results = await Task.WhenAll(first, second);
        Assert.True(results[0].IsSuccess);
        if (secondAmount == 250) {
            Assert.True(results[1].IsSuccess);
            Assert.Equal(results[0].Value, results[1].Value);
        } else {
            Assert.Equal("Hydration.OperationConflict", results[1].Error.Code);
        }
        Assert.Equal(1, await firstContext.HydrationEntries.AsNoTracking().CountAsync(entry => entry.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task Images_ConcurrentConfirmationPublishesOnlyOnce() {
        await using FoodDiaryDbContext firstContext = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"serialized-image-{Guid.NewGuid():N}@example.com", "hash");
        var asset = ImageAsset.Create(user.Id, "serialized/image.jpg", "https://cdn.example.com/image.jpg");
        firstContext.AddRange(user, asset);
        await firstContext.SaveChangesAsync();
        firstContext.ChangeTracker.Clear();
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(firstContext.Database.GetConnectionString()!);
        await using ServiceProvider firstProvider = CreateProvider(firstContext);
        await using ServiceProvider secondProvider = CreateProvider(secondContext);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IImageStorageService storage = Substitute.For<IImageStorageService>();
        storage.ConfirmUploadedObjectAsync(asset.ObjectKey, Arg.Any<CancellationToken>()).Returns(async _ => {
            entered.TrySetResult();
            await release.Task.WaitAsync(timeout.Token);
            return new ImageObjectValidationResult(IsValid: true);
        });
        var command = new ConfirmImageUploadCommand(user.Id.Value, asset.Id.Value);
        Task<Result<ConfirmImageUploadResult>> first = ImageHandler(firstProvider, storage).Handle(command, timeout.Token);
        await entered.Task.WaitAsync(timeout.Token);
        Task<Result<ConfirmImageUploadResult>> second = ImageHandler(secondProvider, storage).Handle(command, timeout.Token);
        release.TrySetResult();
        Result<ConfirmImageUploadResult>[] results = await Task.WhenAll(first, second);
        Assert.All(results, result => Assert.True(result.IsSuccess));
        await storage.Received(1).ConfirmUploadedObjectAsync(asset.ObjectKey, Arg.Any<CancellationToken>());
        await storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        Assert.True((await firstContext.ImageAssets.AsNoTracking().SingleAsync(row => row.Id == asset.Id)).IsConfirmed);
    }

    private static ConfirmImageUploadCommandHandler ImageHandler(IServiceProvider provider, IImageStorageService storage) =>
        new(provider.GetRequiredService<IImageAssetWriteRepository>(), storage, provider.GetRequiredService<IImageObjectDeletionOutbox>(),
            provider.GetRequiredService<IUnitOfWork>(), provider.GetRequiredService<IImageConfirmationTransactionRunner>());

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddSingleton(TimeProvider.System);
        services.AddHydrationModule();
        services.AddImagesInfrastructure();
        services.AddReadModelComposition();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(FoodDiary.Domain.Primitives.IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
