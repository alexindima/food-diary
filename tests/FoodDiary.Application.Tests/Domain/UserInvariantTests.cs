using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Tests.Domain;

public class UserInvariantTests
{
    [Fact]
    public void Create_WithEmptyEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() => User.Create("   ", "hash"));
    }

    [Fact]
    public void UpdateAiTokenLimits_WithNegativeInput_Throws()
    {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateAiTokenLimits(-1, null));
    }

    [Fact]
    public void UpdateAiTokenLimits_WithNegativeOutput_Throws()
    {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateAiTokenLimits(null, -1));
    }
}
