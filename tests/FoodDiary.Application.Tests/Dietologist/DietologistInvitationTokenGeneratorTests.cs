using FoodDiary.Application.Dietologist.Common;

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public sealed class DietologistInvitationTokenGeneratorTests {
    [Fact]
    public void GenerateUrlSafeToken_ReturnsRequestedEntropyWithoutUnsafeCharacters() {
        string token = DietologistInvitationTokenGenerator.GenerateUrlSafeToken(byteLength: 12);

        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
        Assert.Equal(16, token.Length);
    }

    [Fact]
    public void GenerateUrlSafeToken_WithInvalidLength_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DietologistInvitationTokenGenerator.GenerateUrlSafeToken(byteLength: 0));
    }
}
