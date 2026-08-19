using FoodDiary.Infrastructure.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public class PasswordHasherTests {
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString() {
        string hash = _hasher.Hash("password123");

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Hash_ReturnsDifferentHashesForSameInput() {
        string hash1 = _hasher.Hash("password123");
        string hash2 = _hasher.Hash("password123");

        Assert.NotEqual(hash1, hash2, StringComparer.Ordinal);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue() {
        const string password = "MySecurePassword!";
        string hash = _hasher.Hash(password);

        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse() {
        string hash = _hasher.Hash("correct-password");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_ProducesBCryptFormat() {
        string hash = _hasher.Hash("test");

        Assert.StartsWith("$fd$bcrypt-sha384$$2", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_WithPasswordsDifferingAfterLegacyBcryptBoundary_ReturnsFalse() {
        string sharedPrefix = new('a', 72);
        string hash = _hasher.Hash(sharedPrefix + "X");

        Assert.False(_hasher.Verify(sharedPrefix + "Y", hash));
    }

    [Fact]
    public void Verify_WithLegacyBcryptHash_RemainsCompatible() {
        const string password = "legacy-password";
        string legacyHash = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.Multiple(
            () => Assert.True(_hasher.Verify(password, legacyHash)),
            () => Assert.True(_hasher.NeedsRehash(legacyHash)));
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalse() {
        Assert.False(_hasher.Verify("password", "not-a-bcrypt-hash"));
    }

    [Fact]
    public void NeedsRehash_WithEnhancedHash_ReturnsFalse() {
        string hash = _hasher.Hash("password");

        Assert.False(_hasher.NeedsRehash(hash));
    }
}
