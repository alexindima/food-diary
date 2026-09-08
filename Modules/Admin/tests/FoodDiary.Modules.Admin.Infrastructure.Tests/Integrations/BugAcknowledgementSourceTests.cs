using System.Text;
using FoodDiary.Infrastructure.Integrations.MailInbox;

namespace FoodDiary.Modules.Admin.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class BugAcknowledgementSourceTests {
    [Theory]
    [InlineData("Auto-Submitted: auto-replied\r\n")]
    [InlineData("Auto-Submitted: auto-generated\r\n")]
    [InlineData("Precedence: bulk\r\n")]
    [InlineData("List-Id: example.test\r\n")]
    [InlineData("X-Auto-Response-Suppress: All\r\n")]
    [InlineData("In-Reply-To: <original@example.com>\r\n")]
    [InlineData("References: <original@example.com>\r\n")]
    public void AutomaticMailAndRepliesAreNotAcknowledged(string headers) {
        Assert.Null(BugAcknowledgementSource.Parse(Guid.NewGuid(), "person@example.com", Mime(headers)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("<>")]
    [InlineData("someone-else@example.com")]
    public void EmptyOrMismatchedEnvelopeIsRejected(string envelope) {
        Assert.Null(BugAcknowledgementSource.Parse(Guid.NewGuid(), envelope, Mime("")));
    }

    [Fact]
    public void ReimportedMessageUsesSameIdempotencyKey() {
        FoodDiary.Application.Abstractions.Admin.Common.BugAcknowledgementCandidate? first = BugAcknowledgementSource.Parse(Guid.NewGuid(), "person@example.com", Mime(""));
        FoodDiary.Application.Abstractions.Admin.Common.BugAcknowledgementCandidate? second = BugAcknowledgementSource.Parse(Guid.NewGuid(), "person@example.com", Mime(""));
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.IdempotencyKey, second.IdempotencyKey);
        Assert.Equal("person@example.com", first.Recipient);
        Assert.Equal("original@example.com", first.MessageId);
    }

    private static byte[] Mime(string headers) => Encoding.UTF8.GetBytes(
        "From: person@example.com\r\nTo: bugs@fooddiary.club\r\nMessage-ID: <original@example.com>\r\nSubject: Bug\r\n" + headers + "\r\nA problem");
}
