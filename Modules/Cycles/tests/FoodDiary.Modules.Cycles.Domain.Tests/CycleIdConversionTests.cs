using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CycleIdConversionTests {
    [Fact]
    public void CycleIds_PreserveGuidAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");

        AssertId((BleedingEntryId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleConsentId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleDayNoteId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleFactorId)value, value, static id => id, static id => id.ToString());
        AssertId((CyclePredictionRevisionId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleProfileId)value, value, static id => id, static id => id.ToString());
        AssertId((CycleSymptomEntryId)value, value, static id => id, static id => id.ToString());
        AssertId((FertilitySignalId)value, value, static id => id, static id => id.ToString());
        AssertId((MenstrualEpisodeId)value, value, static id => id, static id => id.ToString());
        Assert.Equal(Guid.Empty, CycleConsentId.Empty.Value);
        Assert.Equal(Guid.Empty, CycleDayNoteId.Empty.Value);
    }

    [Fact]
    public void CopiedIds_ReplaceOnlyTheCopyAndPreserveTheOriginalOwnerIdentities() {
        var original = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var replacement = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var bleeding = new BleedingEntryId(original);
        var consent = new CycleConsentId(original);
        var note = new CycleDayNoteId(original);
        var factor = new CycleFactorId(original);
        var revision = new CyclePredictionRevisionId(original);
        var profile = new CycleProfileId(original);
        var symptom = new CycleSymptomEntryId(original);
        var fertility = new FertilitySignalId(original);
        var episode = new MenstrualEpisodeId(original);

        Assert.Multiple(
            () => Assert.Equal(replacement, (bleeding with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (consent with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (note with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (factor with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (revision with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (profile with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (symptom with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (fertility with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (episode with { Value = replacement }).Value),
            () => Assert.Equal(original, bleeding.Value),
            () => Assert.Equal(original, consent.Value),
            () => Assert.Equal(original, note.Value),
            () => Assert.Equal(original, factor.Value),
            () => Assert.Equal(original, revision.Value),
            () => Assert.Equal(original, profile.Value),
            () => Assert.Equal(original, symptom.Value),
            () => Assert.Equal(original, fertility.Value),
            () => Assert.Equal(original, episode.Value));
    }

    private static void AssertId<T>(T id, Guid expected, Func<T, Guid> convert, Func<T, string> format) {
        Assert.Equal(expected, convert(id));
        Assert.Equal(expected.ToString(), format(id));
    }
}
