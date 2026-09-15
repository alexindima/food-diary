using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationContractTests {
    [Fact]
    public void JwtImpersonationContext_StoresActorAndReason() {
        var actorUserId = UserId.New();

        var context = new JwtImpersonationContext(actorUserId, "Support request");

        Assert.Equal(actorUserId, context.ActorUserId);
        Assert.Equal("Support request", context.Reason);
    }

    [Fact]
    public void SecurityTokenGenerator_WithInvalidLength_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => SecurityTokenGenerator.GenerateUrlSafeToken(0));
    }

    [Fact]
    public void SecurityTokenGenerator_ReturnsUrlSafeToken() {
        string token = SecurityTokenGenerator.GenerateUrlSafeToken(32);

        Assert.NotEmpty(token);
        Assert.DoesNotContain("+", token, StringComparison.Ordinal);
        Assert.DoesNotContain("/", token, StringComparison.Ordinal);
        Assert.DoesNotContain("=", token, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SecurityTokenGenerator_NormalizeForSecureHashing_WithBlankToken_Throws(string? token) {
        Assert.Throws<ArgumentException>(() => SecurityTokenGenerator.NormalizeForSecureHashing(token!));
    }

    [Fact]
    public void SecurityTokenGenerator_VerifyFastStorageHash_WithMatchingToken_ReturnsTrue() {
        string storedHash = SecurityTokenGenerator.HashForStorage(" refresh-token ");

        bool isValid = SecurityTokenGenerator.VerifyFastStorageHash("refresh-token", storedHash);

        Assert.True(isValid);
    }

    [Fact]
    public void SecurityTokenGenerator_VerifyFastStorageHash_WithMismatchedToken_ReturnsFalse() {
        string storedHash = SecurityTokenGenerator.HashForStorage("refresh-token");

        bool isValid = SecurityTokenGenerator.VerifyFastStorageHash("other-token", storedHash);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("legacy-hash")]
    public void SecurityTokenGenerator_VerifyFastStorageHash_WithNonFastStorageHash_ReturnsFalse(string? storedHash) {
        bool isValid = SecurityTokenGenerator.VerifyFastStorageHash("refresh-token", storedHash!);

        Assert.False(isValid);
    }
}
