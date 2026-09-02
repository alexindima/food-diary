using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class MealEnumContractTests {
    [Fact]
    public void MealType_PreservesNamesAndNumericValues() {
        Assert.Equal(["Breakfast", "Lunch", "Dinner", "Snack", "Other"], Enum.GetNames<MealType>());
        Assert.Equal([0, 1, 2, 3, 4], Enum.GetValues<MealType>().Select(value => (int)value));
    }

    [Fact]
    public void AiRecognitionSource_PreservesNamesAndNumericValues() {
        Assert.Equal(["Text", "Voice", "Photo"], Enum.GetNames<AiRecognitionSource>());
        Assert.Equal([0, 1, 2], Enum.GetValues<AiRecognitionSource>().Select(value => (int)value));
    }
}
