using FluentValidation.TestHelper;
using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Queries.GetOutgoingEmailJournal;
using FoodDiary.Results;

namespace FoodDiary.MailRelay.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class OutgoingEmailJournalQueryTests {
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(0, null, true)]
    [InlineData(null, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    public void Validator_RequiresExclusiveEndAfterStart(int? from, int? to, bool valid) {
        var query = new GetOutgoingEmailJournalQuery(1, 50, Purpose: null, Status: null, Recipient: null,
            from.HasValue ? DateTimeOffset.UnixEpoch.AddDays(from.Value) : null,
            to.HasValue ? DateTimeOffset.UnixEpoch.AddDays(to.Value) : null);
        Assert.Equal(valid, new GetOutgoingEmailJournalQueryValidator().Validate(query).IsValid);
    }

    [Fact]
    public void Validator_RejectsInvalidPaginationAndOversizedFilters() {
        var query = new GetOutgoingEmailJournalQuery(0, 101, new string('p', 65), new string('s', 33), new string('r', 321), CorrelationId: new string('c', 257));
        TestValidationResult<GetOutgoingEmailJournalQuery> result = new GetOutgoingEmailJournalQueryValidator().TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
        result.ShouldHaveValidationErrorFor(x => x.Limit);
        result.ShouldHaveValidationErrorFor(x => x.Purpose);
        result.ShouldHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.Recipient);
        result.ShouldHaveValidationErrorFor(x => x.CorrelationId);
    }

    [Fact]
    public async Task Handler_PreservesFiltersCancellationAndPage() {
        using var cancellation = new CancellationTokenSource();
        var reader = new RecordingReader();
        var query = new GetOutgoingEmailJournalQuery(2, 10, "welcome", "sent", "to@example.com", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), Guid.NewGuid(), "trace");
        var page = new OutgoingEmailJournalPage([], 31);
        reader.Page = page;
        Result<OutgoingEmailJournalPage> result = await new GetOutgoingEmailJournalQueryHandler(reader).Handle(query, cancellation.Token);
        Assert.True(result.IsSuccess);
        Assert.Same(page, result.Value);
        Assert.Equal(query, reader.Query);
        Assert.Equal(cancellation.Token, reader.Token);
    }
    [ExcludeFromCodeCoverage]
    private sealed class RecordingReader : IMailRelayJournalReader {
        public OutgoingEmailJournalPage Page { get; set; } = new([], 0);
        public GetOutgoingEmailJournalQuery? Query { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<OutgoingEmailJournalPage> GetPageAsync(int page, int limit, string? purpose, string? status, string? recipient, CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? id = null, string? correlationId = null) {
            Query = new GetOutgoingEmailJournalQuery(page, limit, purpose, status, recipient, fromUtc, toUtc, id, correlationId);
            Token = cancellationToken;
            return Task.FromResult(Page);
        }
    }
}
