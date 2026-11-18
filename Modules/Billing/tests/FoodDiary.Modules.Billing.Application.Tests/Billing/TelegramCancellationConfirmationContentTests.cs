using FoodDiary.Modules.Billing.Application.Commands.SendTelegramCancellationConfirmations;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

[ExcludeFromCodeCoverage]
public sealed class TelegramCancellationConfirmationContentTests {
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Render_RejectsAnAmbiguousPremiumEndBeforeFormatting(DateTimeKind kind) {
        var endsAt = new DateTime(2030, 1, 2, 12, 0, 0, kind);

        Assert.Throws<ArgumentException>("accessUntilUtc", () =>
            TelegramCancellationConfirmationContent.Render(endsAt, "ru", "Asia/Tbilisi"));
    }
}
