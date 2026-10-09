using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramOidcValueFormattingTests {
    [Fact]
    public void DiagnosticFormatting_DoesNotExposeOpaqueProtocolValues() {
        var code = new TelegramAuthorizationCode("synthetic-sensitive-code");
        var state = new TelegramOAuthState("synthetic-sensitive-state");
        var nonce = new TelegramOidcNonce("synthetic-sensitive-nonce");
        var verifier = new TelegramPkceVerifier("synthetic-sensitive-verifier");
        string diagnostic = string.Join(" ", code, state, nonce, verifier,
            new TelegramOidcAuthorizationRequest(state, nonce, verifier),
            new TelegramOidcTokenExchange(code, verifier, nonce));

        Assert.Multiple(
            () => Assert.DoesNotContain(code.Value, diagnostic, StringComparison.Ordinal),
            () => Assert.DoesNotContain(state.Value, diagnostic, StringComparison.Ordinal),
            () => Assert.DoesNotContain(nonce.Value, diagnostic, StringComparison.Ordinal),
            () => Assert.DoesNotContain(verifier.Value, diagnostic, StringComparison.Ordinal));
    }
}
