using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class CycleIdConversionTests {
    [Fact]
    public void CycleIds_PreserveGuidAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");

        AssertId((BleedingEntryId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleConsentId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleFactorId)value, value, static id => id, static id => id.ToString());
        AssertId((CyclePredictionRevisionId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleProfileId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleSymptomEntryId)value, value, static id => id, static id => id.ToString());
        AssertId((FertilitySignalId)value, value, static id => id, static id => id.ToString());
        AssertId((MenstrualEpisodeId)value, value, static id => id, static id => id.ToString());
        Assert.Equal(Guid.Empty, CycleConsentId.Empty.Value);
    }

    private static void AssertId<T>(T id, Guid expected, Func<T, Guid> convert, Func<T, string> format) {
        Assert.Equal(expected, convert(id));
        Assert.Equal(expected.ToString(), format(id));
    }
}
