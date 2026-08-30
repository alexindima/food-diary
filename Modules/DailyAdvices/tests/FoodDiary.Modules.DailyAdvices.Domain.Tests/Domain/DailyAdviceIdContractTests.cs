using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.DailyAdvices.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DailyAdviceIdContractTests {
    [Fact]
    public void DailyAdviceId_PreservesGuidAcrossConversionsAndFormatting() {
        var value = Guid.Parse("cd8a02e8-ce9c-4d41-97b4-0303485ca28c");
        var id = (DailyAdviceId)value;
        Guid converted = id;

        Assert.Multiple(
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value, converted),
            () => Assert.Equal(value.ToString(), id.ToString()));
    }
}
