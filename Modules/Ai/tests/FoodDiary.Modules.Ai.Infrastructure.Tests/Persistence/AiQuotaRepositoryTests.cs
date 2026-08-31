using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Ai;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class AiQuotaRepositoryTests {
    private static readonly DateTime Now = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodStartUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReserveAsync_WithRequestIdOfWrongLength_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with { RequestId = new string('a', 63) };

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateRepository().ReserveAsync(request));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task ReserveAsync_WithNonHexRequestId_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with {
            RequestId = string.Concat(new string('a', 63), "g"),
        };

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateRepository().ReserveAsync(request));

        Assert.Equal("request", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ReserveAsync_WithBlankOperation_ThrowsArgumentException(string operation) {
        AiQuotaReservationRequest request = CreateValidRequest() with { Operation = operation };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Fact]
    public async Task ReserveAsync_WithOperationLongerThanLimit_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with { Operation = new string('o', 33) };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReserveAsync_WithNonPositiveInputTokens_ThrowsArgumentException(long inputTokens) {
        AiQuotaReservationRequest request = CreateValidRequest() with { InputTokens = inputTokens };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReserveAsync_WithNonPositiveOutputTokens_ThrowsArgumentException(long outputTokens) {
        AiQuotaReservationRequest request = CreateValidRequest() with { OutputTokens = outputTokens };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Fact]
    public async Task ReserveAsync_WithNegativeInputTokenLimit_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with { InputTokenLimit = -1 };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Fact]
    public async Task ReserveAsync_WithNegativeOutputTokenLimit_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with { OutputTokenLimit = -1 };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Fact]
    public async Task ReserveAsync_WithNonUtcPeriodStart_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with {
            PeriodStartUtc = DateTime.SpecifyKind(PeriodStartUtc, DateTimeKind.Local),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Fact]
    public async Task ReserveAsync_WithPeriodStartNotOnFirstOfMonth_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with {
            PeriodStartUtc = PeriodStartUtc.AddDays(1),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Fact]
    public async Task ReserveAsync_WithPeriodStartHavingTimeOfDay_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with {
            PeriodStartUtc = PeriodStartUtc.AddHours(1),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Fact]
    public async Task ReserveAsync_WithNonUtcExpiresOn_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with {
            ExpiresOnUtc = DateTime.SpecifyKind(Now.AddMinutes(30), DateTimeKind.Local),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    [Fact]
    public async Task ReserveAsync_WithExpiresOnAtOrBeforeNow_ThrowsArgumentException() {
        AiQuotaReservationRequest request = CreateValidRequest() with { ExpiresOnUtc = Now };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateRepository().ReserveAsync(request));
    }

    private static AiQuotaRepository CreateRepository() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AiQuotaRepository(options, new FixedTimeProvider(Now));
    }

    private static AiQuotaReservationRequest CreateValidRequest() =>
        new(
            new string('a', 64),
            UserId.New(),
            PeriodStartUtc,
            "nutrition",
            InputTokens: 100,
            OutputTokens: 50,
            InputTokenLimit: 1_000,
            OutputTokenLimit: 1_000,
            Now.AddMinutes(30));

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
