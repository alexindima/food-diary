using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.DisconnectTelegram;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class DisconnectTelegramCommandContractTests {
    [Fact]
    public void DiagnosticRepresentationsOmitTheConfirmationCredentials() {
        const string password = "test-password";
        const string googleCredential = "test-google-credential";
        var command = new DisconnectTelegramCommand(Guid.NewGuid(), SecurityVersion: 12, password, googleCredential,
            new AuthenticationClientContext("telegram-disconnect", IpAddress: null, UserAgent: null), RememberMe: false);

        Assert.Equal(nameof(DisconnectTelegramCommand), command.ToString());
        string serialized = JsonSerializer.Serialize(command);
        Assert.DoesNotContain(password, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(googleCredential, serialized, StringComparison.Ordinal);
    }
}
