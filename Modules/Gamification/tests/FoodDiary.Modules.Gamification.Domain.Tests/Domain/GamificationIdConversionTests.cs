using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class GamificationIdConversionTests {
    [Fact]
    public void GamificationIds_PreserveGuidAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var definition = (AchievementDefinitionId)value;
        var achievement = (UserAchievementId)value;

        Assert.Multiple(
            () => Assert.Equal(value, (Guid)definition),
            () => Assert.Equal(value.ToString(), definition.ToString()),
            () => Assert.Equal(Guid.Empty, AchievementDefinitionId.Empty.Value),
            () => Assert.Equal(value, (Guid)achievement),
            () => Assert.Equal(value.ToString(), achievement.ToString()));
    }
}
