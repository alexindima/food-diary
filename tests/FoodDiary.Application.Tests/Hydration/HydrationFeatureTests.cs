using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Hydration.Models;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Hydration.Services;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Tests.Hydration;

[ExcludeFromCodeCoverage]
public class HydrationFeatureTests {
    [Fact]
    public async Task CreateHydrationEntryCommandValidator_WithEmptyUserId_Fails() {
        var validator = new CreateHydrationEntryCommandValidator();
        var command = new CreateHydrationEntryCommand(Guid.Empty, DateTime.UtcNow, 250);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandValidator_WithEmptyEntryId_Fails() {
        var validator = new DeleteHydrationEntryCommandValidator();
        var command = new DeleteHydrationEntryCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandValidator_WithInvalidAmount_Fails() {
        var validator = new UpdateHydrationEntryCommandValidator();
        var command = new UpdateHydrationEntryCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 0);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryValidator_WithValidUserId_Passes() {
        var validator = new GetHydrationDailyTotalQueryValidator();
        var query = new GetHydrationDailyTotalQuery(Guid.NewGuid(), DateTime.UtcNow);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetHydrationEntriesQueryValidator();
        var query = new GetHydrationEntriesQuery(Guid.Empty, DateTime.UtcNow);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void HydrationValidators_ValidateAmount_WithOutOfRangeValue_Fails(int amountMl) {
        Result result = HydrationValidators.ValidateAmount(amountMl);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void HydrationValidators_ValidateAmount_WithValidValue_Passes() {
        Result result = HydrationValidators.ValidateAmount(500);

        ResultAssert.Success(result);
    }

    [Fact]
    public async Task HydrationGoalService_WhenUserIsMissing_ReturnsInvalidToken() {
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService.GetAccessibleUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User>(Errors.Authentication.InvalidToken));
        var service = new HydrationGoalService(userContextService);

        Result<double?> result = await service.GetCurrentGoalAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryHandler_WithUnspecifiedDate_PreservesCalendarDayAsUtc() {
        var user = User.Create("user@example.com", "hash");
        var repository = new RecordingHydrationEntryRepository();
        IHydrationGoalService hydrationGoalService = CreateHydrationGoalService(user);
        var handler = new GetHydrationDailyTotalQueryHandler(
            repository,
            hydrationGoalService,
            CreateCurrentUserAccessService(user));
        var queryDate = new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Unspecified);

        Result<HydrationDailyModel> result = await handler.Handle(
            new GetHydrationDailyTotalQuery(user.Id.Value, queryDate),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc), repository.LastDailyTotalDateUtc);
        Assert.Equal(new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc), result.Value.DateUtc);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetHydrationDailyTotalQueryHandler(
            new RecordingHydrationEntryRepository(),
            CreateHydrationGoalService(User.Create("user@example.com", "hash")),
            CreateCurrentUserAccessService(User.Create("hydration-total-empty@example.com", "hash")));

        Result<HydrationDailyModel> result = await handler.Handle(
            new GetHydrationDailyTotalQuery(Guid.Empty, DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-hydration@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetHydrationDailyTotalQueryHandler(
            new RecordingHydrationEntryRepository(),
            CreateHydrationGoalService(user),
            CreateCurrentUserAccessService(user));

        Result<HydrationDailyModel> result = await handler.Handle(
            new GetHydrationDailyTotalQuery(user.Id.Value, DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryHandler_WhenUserCannotAccess_DoesNotReadGoal() {
        var user = User.Create("hydration-total-denied@example.com", "hash");
        IHydrationGoalService hydrationGoalService = Substitute.For<IHydrationGoalService>();
        var handler = new GetHydrationDailyTotalQueryHandler(
            new RecordingHydrationEntryRepository(),
            hydrationGoalService,
            CreateCurrentUserAccessService(User.Create("hydration-total-other@example.com", "hash")));

        Result<HydrationDailyModel> result = await handler.Handle(
            new GetHydrationDailyTotalQuery(user.Id.Value, DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
        await hydrationGoalService
            .DidNotReceive()
            .GetCurrentGoalAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateHydrationEntryCommandHandler_WithUnspecifiedTimestamp_PreservesInstantAsUtc() {
        var user = User.Create("hydration-create@example.com", "hash");
        var repository = new InMemoryHydrationEntryRepository();
        var handler = new CreateHydrationEntryCommandHandler(repository, CreateCurrentUserAccessService(user));
        var timestamp = new DateTime(2026, 3, 26, 14, 30, 0, DateTimeKind.Unspecified);

        Result<HydrationEntryModel> result = await handler.Handle(
            new CreateHydrationEntryCommand(user.Id.Value, timestamp, 250),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DateTimeKind.Utc, result.Value.TimestampUtc.Kind);
        Assert.Equal(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc), result.Value.TimestampUtc);
        Assert.Equal(result.Value.TimestampUtc, repository.AddedEntry?.Timestamp);
    }

    [Fact]
    public async Task CreateHydrationEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new CreateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(User.Create("hydration-create-empty@example.com", "hash")));

        Result<HydrationEntryModel> result = await handler.Handle(
            new CreateHydrationEntryCommand(Guid.Empty, DateTime.UtcNow, 250),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateHydrationEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("hydration-create-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new CreateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new CreateHydrationEntryCommand(user.Id.Value, DateTime.UtcNow, 250),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateHydrationEntryCommandHandler_WithInvalidAmount_ReturnsValidationFailure() {
        var user = User.Create("hydration-create-invalid@example.com", "hash");
        var handler = new CreateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new CreateHydrationEntryCommand(user.Id.Value, DateTime.UtcNow, 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithUnspecifiedTimestamp_PreservesInstantAsUtc() {
        var user = User.Create("hydration-update@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc), 250);
        var repository = new InMemoryHydrationEntryRepository(entry);
        var handler = new UpdateHydrationEntryCommandHandler(repository, CreateCurrentUserAccessService(user));
        var timestamp = new DateTime(2026, 3, 26, 14, 30, 0, DateTimeKind.Unspecified);

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, timestamp, AmountMl: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DateTimeKind.Utc, result.Value.TimestampUtc.Kind);
        Assert.Equal(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc), result.Value.TimestampUtc);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithValidAmount_UpdatesEntry() {
        var user = User.Create("hydration-update-amount@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow.AddHours(-1), 250);
        var repository = new InMemoryHydrationEntryRepository(entry);
        var handler = new UpdateHydrationEntryCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, DateTime.UtcNow, 750),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(750, result.Value.AmountMl);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(User.Create("hydration-update-empty@example.com", "hash")));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(Guid.Empty, Guid.NewGuid(), DateTime.UtcNow, 250),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("hydration-update-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateCurrentUserAccessService(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, DateTime.UtcNow, 500),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WhenEntryMissing_ReturnsNotAccessible() {
        var user = User.Create("hydration-update-missing@example.com", "hash");
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(user));
        var entryId = Guid.NewGuid();

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entryId, DateTime.UtcNow, 500),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("HydrationEntry.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithEntryFromOtherUser_ReturnsNotAccessible() {
        var user = User.Create("hydration-update-owner@example.com", "hash");
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateCurrentUserAccessService(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, DateTime.UtcNow, 500),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("HydrationEntry.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithInvalidAmount_ReturnsValidationFailure() {
        var user = User.Create("hydration-update-invalid@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateCurrentUserAccessService(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, DateTime.UtcNow, 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithEmptyHydrationEntryId_ReturnsValidationFailure() {
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("HydrationEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(User.Create("hydration-delete-empty@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("hydration-delete-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WhenEntryMissing_ReturnsNotAccessible() {
        var user = User.Create("hydration-delete-missing@example.com", "hash");
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("HydrationEntry.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithEntryFromOtherUser_ReturnsNotAccessible() {
        var user = User.Create("hydration-delete-owner@example.com", "hash");
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("HydrationEntry.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithOwnedEntry_DeletesEntry() {
        var user = User.Create("hydration-delete-success@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var repository = new InMemoryHydrationEntryRepository(entry);
        var handler = new DeleteHydrationEntryCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Same(entry, repository.DeletedEntry);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithEmptyHydrationEntryId_ReturnsValidationFailure() {
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 250),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("HydrationEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryHandler_WithInvalidUserId_ReturnsInvalidToken() {
        var handler = new GetHydrationEntriesQueryHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(User.Create("hydration-entries@example.com", "hash")));

        Result<IReadOnlyList<HydrationEntryModel>> result = await handler.Handle(new GetHydrationEntriesQuery(Guid.Empty, DateTime.UtcNow), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-hydration-entries@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetHydrationEntriesQueryHandler(
            new InMemoryHydrationEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<HydrationEntryModel>> result = await handler.Handle(new GetHydrationEntriesQuery(user.Id.Value, DateTime.UtcNow), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryHandler_NormalizesDateAndMapsEntries() {
        var user = User.Create("hydration-entries-date@example.com", "hash");
        var repository = new InMemoryHydrationEntryRepository();
        HydrationEntry entry = await repository.AddAsync(HydrationEntry.Create(
            user.Id,
            new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc),
            350));
        await repository.AddAsync(HydrationEntry.Create(
            UserId.New(),
            new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc),
            250));
        var handler = new GetHydrationEntriesQueryHandler(repository, CreateCurrentUserAccessService(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);

        Result<IReadOnlyList<HydrationEntryModel>> result = await handler.Handle(new GetHydrationEntriesQuery(user.Id.Value, dateOnly), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc), repository.LastGetByDateDateUtc);
        HydrationEntryModel model = Assert.Single(result.Value);
        Assert.Equal(entry.Id.Value, model.Id);
        Assert.Equal(350, model.AmountMl);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHydrationEntryRepository : IHydrationEntryRepository, IHydrationEntryReadService {
        public DateTime? LastDailyTotalDateUtc { get; private set; }

        public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<HydrationEntry?> GetByIdAsync(HydrationEntryId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HydrationEntry>>([]);

        public Task<IReadOnlyList<HydrationEntryReadModel>> GetByDateReadModelsAsync(
            UserId userId,
            DateTime dateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HydrationEntryReadModel>>([]);

        public Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) {
            LastDailyTotalDateUtc = dateUtc;
            return Task.FromResult(0);
        }

        public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        async Task<IReadOnlyList<HydrationEntryModel>> IHydrationEntryReadService.GetEntriesByDateAsync(
            UserId userId,
            DateTime dateUtc,
            CancellationToken cancellationToken) {
            IReadOnlyList<HydrationEntry> entries = await GetByDateAsync(userId, dateUtc, cancellationToken).ConfigureAwait(false);
            return [.. entries.Select(entry => entry.ToModel())];
        }

        Task<int> IHydrationEntryReadService.GetDailyTotalAsync(
            UserId userId,
            DateTime dateUtc,
            CancellationToken cancellationToken) =>
            GetDailyTotalAsync(userId, dateUtc, cancellationToken);

        Task<IReadOnlyList<(DateTime Date, int TotalMl)>> IHydrationEntryReadService.GetDailyTotalsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken) =>
            GetDailyTotalsAsync(userId, dateFrom, dateTo, cancellationToken);
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        Error? error = user.DeletedAt is null ? null : Errors.Authentication.AccountDeleted;
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user.Id == id ? error : Errors.Authentication.InvalidToken);
            });
        return service;
    }

    private static IHydrationGoalService CreateHydrationGoalService(User user) {
        IHydrationGoalService service = Substitute.For<IHydrationGoalService>();
        Error? error = user.DeletedAt is null ? null : Errors.Authentication.AccountDeleted;
        service
            .GetCurrentGoalAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user.Id == id
                    ? Result.Success(user.HydrationGoal ?? user.WaterGoal)
                    : Result.Failure<double?>(Errors.Authentication.InvalidToken));
            });

        if (error is not null) {
            service
                .GetCurrentGoalAsync(user.Id, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Failure<double?>(error)));
        }

        return service;
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryHydrationEntryRepository(HydrationEntry? entry = null) : IHydrationEntryRepository, IHydrationEntryReadService {
        private HydrationEntry? _entry = entry;
        private readonly List<HydrationEntry> _entries = entry is null ? [] : [entry];
        public HydrationEntry? AddedEntry { get; private set; }
        public HydrationEntry? DeletedEntry { get; private set; }
        public DateTime? LastGetByDateDateUtc { get; private set; }

        public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
            AddedEntry = entry;
            _entry = entry;
            _entries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
            _entry = entry;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
            DeletedEntry = entry;
            _entry = null;
            _entries.Remove(entry);
            return Task.CompletedTask;
        }

        public Task<HydrationEntry?> GetByIdAsync(HydrationEntryId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_entry?.Id == id ? _entry : null);

        public Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) {
            LastGetByDateDateUtc = dateUtc;
            var entries = _entries
                .Where(entry => entry.UserId == userId && entry.Timestamp.Date == dateUtc.Date)
                .ToList();
            return Task.FromResult<IReadOnlyList<HydrationEntry>>(entries);
        }

        public async Task<IReadOnlyList<HydrationEntryReadModel>> GetByDateReadModelsAsync(
            UserId userId,
            DateTime dateUtc,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<HydrationEntry> entries = await GetByDateAsync(userId, dateUtc, cancellationToken).ConfigureAwait(false);
            return [.. entries.Select(entry => new HydrationEntryReadModel(entry.Id.Value, entry.Timestamp, entry.AmountMl))];
        }

        public Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(DateTime, int)>>([]);

        async Task<IReadOnlyList<HydrationEntryModel>> IHydrationEntryReadService.GetEntriesByDateAsync(
            UserId userId,
            DateTime dateUtc,
            CancellationToken cancellationToken) {
            IReadOnlyList<HydrationEntry> entries = await GetByDateAsync(userId, dateUtc, cancellationToken).ConfigureAwait(false);
            return [.. entries.Select(entry => entry.ToModel())];
        }

        Task<int> IHydrationEntryReadService.GetDailyTotalAsync(
            UserId userId,
            DateTime dateUtc,
            CancellationToken cancellationToken) =>
            GetDailyTotalAsync(userId, dateUtc, cancellationToken);

        Task<IReadOnlyList<(DateTime Date, int TotalMl)>> IHydrationEntryReadService.GetDailyTotalsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken) =>
            GetDailyTotalsAsync(userId, dateFrom, dateTo, cancellationToken);
    }
}
