using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class CycleEnumContractTests {
    [Fact]
    public void BleedingType_PreservesPersistedValues() {
        Assert.Equal([("Bleeding", 0), ("Spotting", 1)], ReadValues<BleedingType>());
    }

    [Fact]
    public void CycleSymptomCategory_PreservesPersistedValues() {
        Assert.Equal([
            ("Pain", 0), ("Mood", 1), ("Energy", 2), ("Sleep", 3),
            ("Appetite", 4), ("Craving", 5), ("Bloating", 6), ("Headache", 7),
            ("Skin", 8), ("Stool", 9), ("Nausea", 10), ("Libido", 11), ("Other", 99),
        ], ReadValues<CycleSymptomCategory>());
    }

    [Fact]
    public void OvulationTestResult_PreservesPersistedValues() {
        Assert.Equal([("Negative", 0), ("Positive", 1), ("Unknown", 2)], ReadValues<OvulationTestResult>());
    }

    private static (string Name, int Value)[] ReadValues<TEnum>() where TEnum : struct, Enum =>
        [.. Enum.GetValues<TEnum>().Select(value => (value.ToString(), Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)))];
}
