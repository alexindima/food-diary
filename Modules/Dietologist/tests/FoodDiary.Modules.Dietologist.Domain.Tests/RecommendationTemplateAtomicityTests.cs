using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecommendationTemplateAtomicityTests {
    [Fact]
    public void EntityUpdates_WhenLateValidationFails_AreAtomic() {
        var template = RecommendationTemplate.Create(UserId.New(), "Original", "Original text");
        Assert.Throws<ArgumentOutOfRangeException>(() => template.Update("Changed", new string('x', 2001)));
        Assert.Multiple(
            () => Assert.Equal("Original", template.Name));
    }
}
