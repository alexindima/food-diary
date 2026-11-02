using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CreateBrowserSession;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.LogoutBrowserSession;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.RestoreBrowserSession;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Queries.AuthenticateBrowserSession;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Application.Mappings;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionRedactionTests {
    [Fact]
    public void DiagnosticsAndJson_DoNotExposeBrowserCredentialsOrTheirStorageHash() {
        const string credential = "synthetic-browser-credential-for-redaction";
        const string hash = "synthetic-storage-hash-for-redaction";
        var user = User.Create("redaction@example.test", "synthetic-password-hash");
        var principal = new UserAuthenticationPrincipalModel(user.Id, user.Email, [], AccessTokenCapUtc: null, user.ToModel());
        DateTime now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        object[] values = [
            new CreateBrowserSessionCommand(credential, hash),
            new LogoutBrowserSessionCommand(credential),
            new RestoreBrowserSessionCommand(credential),
            new AuthenticateBrowserSessionQuery(credential),
            new BrowserSessionModel(credential, Guid.NewGuid(), now, RememberMe: false, principal),
            new BrowserSessionProof(RefreshTokenSessionId.New(), user.Id, hash, SecurityVersion: 1,
                now, RevokedAtUtc: null, now),
        ];

        Assert.All(values, value => {
            string display = value.ToString()!;
            string json = JsonSerializer.Serialize(value, value.GetType());
            Assert.Multiple(
                () => Assert.Contains("[redacted]", display, StringComparison.Ordinal),
                () => Assert.DoesNotContain(credential, display, StringComparison.Ordinal),
                () => Assert.DoesNotContain(hash, display, StringComparison.Ordinal),
                () => Assert.DoesNotContain(credential, json, StringComparison.Ordinal),
                () => Assert.DoesNotContain(hash, json, StringComparison.Ordinal));
        });
    }
}
