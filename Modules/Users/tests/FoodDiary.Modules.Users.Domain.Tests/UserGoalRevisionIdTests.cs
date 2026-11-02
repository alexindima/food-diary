using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserGoalRevisionIdTests {
    [Fact]
    public void CopyAndConversionsPreserveTheRevisionIdentity() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        UserGoalRevisionId original = UserGoalRevisionId.Empty;
        UserGoalRevisionId copy = original with { Value = value };
        Guid roundTrip = copy;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal((UserGoalRevisionId)value, copy),
            () => Assert.Equal(Guid.Empty, original.Value),
            () => Assert.NotEqual(Guid.Empty, UserGoalRevisionId.New().Value));
    }
}
