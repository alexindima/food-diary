using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessagePage;
using FoodDiary.MailInbox.Presentation.Features.Messages;
using FoodDiary.MailInbox.Presentation.Features.Messages.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Presentation.Tests;

public sealed partial class MailInboxPresentationTests {
    [Fact]
    public async Task GetPage_ForwardsExtendedFiltersAndMapsCounters() {
        var query = new GetInboundMailMessagePageQuery(2, 10, "to@example.com", "general", Unread: true, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), "subject", "from@example.com", Guid.NewGuid());
        var entry = new InboundMailMessageSummary(Guid.NewGuid(), "from@example.com", ["to@example.com"], "subject", "general", "received", ReadAtUtc: null, DateTimeOffset.UnixEpoch);
        var page = new InboundMailMessagePage([entry], 31, 11, 20);
        StubSender sender = new StubSender().Register(query, Result.Success(page));
        MailInboxMessagesController controller = CreateMessagesController(sender);
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetPage(2, 10, query.Recipient, query.Category, query.Unread, query.FromUtc, query.ToUtc, query.Search, query.FromAddress, query.Id));
        InboundMailMessagePageHttpResponse response = Assert.IsType<InboundMailMessagePageHttpResponse>(result.Value);
        Assert.Equivalent(page, response, strict: true);
        Assert.Equal(query, sender.LastRequest);
    }
}
