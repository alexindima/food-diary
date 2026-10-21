using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DefaultClientTaskPaginationTests {
    [Theory]
    [InlineData(false, 2, 2, 2)]
    [InlineData(true, 2, 2, 2)]
    [InlineData(false, 4, 2, 0)]
    [InlineData(true, 4, 2, 0)]
    [InlineData(false, 1, 0, 0)]
    [InlineData(true, 1, 0, 0)]
    public async Task CompatibilityPages_KeepOrderAndForwardClientDietologistAndCancellation(bool forDietologist, int page, int limit, int expectedCount) {
        var client = UserId.New();
        var dietologist = UserId.New();
        ClientTaskReadModel[] rows = [.. Enumerable.Range(0, 5).Select(index => new ClientTaskReadModel(
            Guid.NewGuid(), dietologist.Value, client.Value, "Task", Details: null, DueAtUtc: null, ClientTaskStatus.Open,
            new DateTime(2026, 10, 21, 12, index, 0, DateTimeKind.Utc), StatusChangedAtUtc: null))];
        var reader = new LegacyTaskReader(rows);
        IClientTaskReadModelRepository repository = reader;
        using var cancellation = new CancellationTokenSource();

        IReadOnlyList<ClientTaskReadModel> result = forDietologist
            ? await repository.GetByDietologistAndClientAsync(dietologist, client, page, limit, cancellation.Token)
            : await repository.GetByClientAsync(client, page, limit, cancellation.Token);

        (UserId? Dietologist, UserId Client, CancellationToken Token) read = Assert.Single(reader.Reads);
        Assert.Equal((forDietologist ? dietologist : (UserId?)null, client, cancellation.Token), read);
        Assert.Equal(expectedCount, result.Count);
        if (expectedCount > 0) { Assert.Same(rows[2], result[0]); Assert.Same(rows[3], result[1]); }
    }

    [Fact]
    public async Task MissingImplementation_ReportsUnsupportedReadsInsteadOfAnEmptyList() {
        IClientTaskReadModelRepository repository = new MissingTaskReader();
        var client = UserId.New();
        var dietologist = UserId.New();

        Action clientRead = () => _ = repository.GetByClientAsync(client, CancellationToken.None);
        Action dietologistRead = () => _ = repository.GetByDietologistAndClientAsync(dietologist, client, CancellationToken.None);
        Assert.Throws<NotSupportedException>(clientRead);
        Assert.Throws<NotSupportedException>(dietologistRead);
        await Assert.ThrowsAsync<NotSupportedException>(() => repository.GetByClientAsync(client, 1, 10, CancellationToken.None));
        await Assert.ThrowsAsync<NotSupportedException>(() => repository.GetByDietologistAndClientAsync(dietologist, client, 1, 10, CancellationToken.None));
    }

    [ExcludeFromCodeCoverage]
    private sealed class LegacyTaskReader(IReadOnlyList<ClientTaskReadModel> rows) : IClientTaskReadModelRepository {
        public List<(UserId? Dietologist, UserId Client, CancellationToken Token)> Reads { get; } = [];

        public Task<IReadOnlyList<ClientTaskReadModel>> GetByClientAsync(UserId clientUserId, CancellationToken cancellationToken = default) {
            Reads.Add((null, clientUserId, cancellationToken));
            return Task.FromResult(rows);
        }

        public Task<IReadOnlyList<ClientTaskReadModel>> GetByDietologistAndClientAsync(UserId dietologistUserId, UserId clientUserId, CancellationToken cancellationToken = default) {
            Reads.Add((dietologistUserId, clientUserId, cancellationToken));
            return Task.FromResult(rows);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class MissingTaskReader : IClientTaskReadModelRepository;
}
