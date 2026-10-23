using FoodDiary.MailInbox.Application.Messages.Models;

namespace FoodDiary.MailInbox.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdmissionCopyContractTests {
    [Fact]
    public void Copy_CanCarryRelayMetadataWithoutChangingThePublishedUntrustedDefault() {
        InboundMailAdmission original = InboundMailAdmission.Untrusted;

        InboundMailAdmission copied = original with { IsTrustedRelay = true, EnvelopeFromAddress = "relay@example.test" };

        Assert.Multiple(
            () => Assert.False(original.IsTrustedRelay),
            () => Assert.Null(original.EnvelopeFromAddress),
            () => Assert.True(copied.IsTrustedRelay),
            () => Assert.Equal("relay@example.test", copied.EnvelopeFromAddress),
            () => Assert.Equal(new InboundMailAdmission(IsTrustedRelay: true, "relay@example.test"), copied),
            () => Assert.Equal(original, InboundMailAdmission.Untrusted));
    }
}
