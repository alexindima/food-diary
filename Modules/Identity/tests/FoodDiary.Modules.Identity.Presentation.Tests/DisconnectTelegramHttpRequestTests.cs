using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class DisconnectTelegramHttpRequestTests {
    [Fact]
    public void DiagnosticTextOmitsBothConfirmationCredentials() {
        var request = new DisconnectTelegramHttpRequest(Password: "test-password", GoogleCredential: "test-google-credential");

        Assert.Equal(nameof(DisconnectTelegramHttpRequest), request.ToString());
    }
}
