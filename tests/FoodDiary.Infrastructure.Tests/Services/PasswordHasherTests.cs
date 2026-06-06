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

        Assert.NotEqual(hash1, hash2);
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

        Assert.StartsWith("$2", hash, StringComparison.Ordinal);
    }
}
