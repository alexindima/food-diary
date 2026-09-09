using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Integrations.Services;
using FoodDiary.MailRelay.Client.Journal;

namespace FoodDiary.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class RelayEmailJournalTests {
    [Fact]
    public async Task GetPage_PreservesFiltersDeliveryMetadataAndCounters() {
        using var cancellation = new CancellationTokenSource();
        IMailRelayJournalClient client = Substitute.For<IMailRelayJournalClient>();
        var id = Guid.NewGuid();
        var entry = new FoodDiary.MailRelay.Client.Models.OutgoingEmailJournalEntry(id, "sent", "welcome", "from@example.com", ["to@example.com"], "subject", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(1), 1, 3, "trace", "body", ContentHidden: false, "reply@example.com", "message-id");
        var page = new FoodDiary.MailRelay.Client.Models.OutgoingEmailJournalPage([entry], 31, new Dictionary<string, long>(StringComparer.Ordinal) { ["sent"] = 31 });
        client.GetPageAsync(2, 10, "welcome", "sent", "to@example.com", cancellation.Token, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), id, "trace").Returns(page);
        var journal = new RelayEmailJournal(client);
        OutgoingEmailJournalPage actual = await journal.GetPageAsync(2, 10, "welcome", "sent", "to@example.com", cancellation.Token, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), id, "trace");
        Assert.Equivalent(page, actual, strict: true);
        await client.Received(1).GetPageAsync(2, 10, "welcome", "sent", "to@example.com", cancellation.Token, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), id, "trace");
    }
}
