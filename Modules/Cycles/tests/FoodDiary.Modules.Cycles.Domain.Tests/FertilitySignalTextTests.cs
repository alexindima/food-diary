using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FertilitySignalTextTests {
    [Theory]
    [InlineData(129)]
    [InlineData(501)]
    public void Create_RejectsFluidTooLongForPersistence(int length) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(new string('x', length)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_NormalizesMissingFluid(string? value) {
        Assert.Null(Create(value).CervicalFluid);
    }

    [Fact]
    public void Create_TrimsFluidAtPersistenceBoundary() {
        string text = new('x', 128);
        Assert.Equal(text, Create("  " + text + "  ").CervicalFluid);
    }

    [Fact]
    public void Update_RejectsOverlongFluidWithoutChangingExistingValues() {
        FertilitySignal signal = Create("saved");
        Assert.Throws<ArgumentOutOfRangeException>(() => signal.Update(basalBodyTemperatureCelsius: 42, ovulationTestResult: null, cervicalFluid: new string('x', 129), hadSex: true, notes: "changed", clearNotes: false));
        Assert.Multiple(
            () => Assert.Equal("saved", signal.CervicalFluid),
            () => Assert.Equal(36.62, signal.BasalBodyTemperatureCelsius),
            () => Assert.Equal("note", signal.Notes),
            () => Assert.False(signal.HadSex));
    }

    private static FertilitySignal Create(string? fluid) =>
        FertilitySignal.Create(CycleProfileId.New(), new DateOnly(2026, 4, 2), basalBodyTemperatureCelsius: 36.62, ovulationTestResult: null, cervicalFluid: fluid, hadSex: false, notes: "note");
}
