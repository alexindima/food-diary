using FoodDiary.MailRelay.Application.Emails.Queries.GetOutgoingEmailJournal;
using FoodDiary.MailRelay.Presentation.Features.Email;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Tests;

public sealed partial class MailRelayPresentationTests {
    [Fact]
    public async Task Journal_ForwardsFiltersAndMapsDeliveryMetadata() {
        var entry = new FoodDiary.MailRelay.Application.Emails.Models.OutgoingEmailJournalEntry(Guid.NewGuid(), "sent", "welcome", "from@example.com", ["to@example.com"], "subject", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(1), 1, 3, "trace", "body", ContentHidden: false, "reply@example.com", "message-id");
        var page = new FoodDiary.MailRelay.Application.Emails.Models.OutgoingEmailJournalPage([entry], 31);
        var sender = new RecordingSender { JournalResult = Result.Success(page) };
        var controller = new MailRelayMessagesController(sender) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var request = new GetOutgoingEmailJournalHttpQuery(2, 10, "welcome", "sent", "to@example.com", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), entry.Id, "trace");
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetPage(request));
        FoodDiary.MailRelay.Client.Models.OutgoingEmailJournalPage response = Assert.IsType<FoodDiary.MailRelay.Client.Models.OutgoingEmailJournalPage>(result.Value);
        GetOutgoingEmailJournalQuery query = Assert.IsType<GetOutgoingEmailJournalQuery>(sender.LastRequest);
        Assert.Multiple(() => Assert.Equivalent(page, response, strict: true), () => Assert.Equivalent(request, query, strict: true));
    }
}
