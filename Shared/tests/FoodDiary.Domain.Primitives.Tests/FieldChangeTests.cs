
namespace FoodDiary.Domain.Primitives.Tests;

[ExcludeFromCodeCoverage]
public sealed class FieldChangeTests {
    [Fact]
    public void Changes_DistinguishOmissionValueAndClearing() {
        FieldChange<string> omitted = FieldChanges.Unchanged<string>();
        FieldChange<string> clear = FieldChanges.Clear<string>();
        FieldChange<string> value = FieldChanges.Set("new");
        Assert.Multiple(
            () => Assert.True(omitted.IsUnchanged),
            () => Assert.True(clear.IsClear),
            () => Assert.True(value.IsSet),
            () => Assert.Equal("new", value.Value),
            () => Assert.NotEqual(omitted, clear));
        Assert.Throws<InvalidOperationException>(() => omitted.Value);
        Assert.Throws<InvalidOperationException>(() => clear.Value);
    }

    [Fact]
    public void Boundaries_KeepBlankTextDistinctFromOmissionAndRejectConflictingOperations() {
        Assert.True(FieldChanges.FromOptionalText(value: null, clear: false).IsUnchanged);
        Assert.Equal("  ", FieldChanges.FromOptionalText("  ", clear: false).Value);
        Assert.True(FieldChanges.FromOptionalText("  ", clear: true).IsClear);
        Assert.Throws<ArgumentException>(() => FieldChanges.FromOptionalText("set", clear: true));
        Assert.Throws<ArgumentException>(() => FieldChanges.FromOptionalValue<int>(1, clear: true));
        Assert.Throws<ArgumentNullException>(() => FieldChanges.Set<string>(null!));
    }

    [Fact]
    public void DefaultNumericValue_IsAnExplicitSetRatherThanAbsence() {
        FieldChange<int> zero = FieldChanges.Set(0);
        Assert.True(zero.IsSet);
        Assert.Equal(0, zero.Value);
        Assert.True(FieldChanges.FromOptionalValue<int>(value: null, clear: false).IsUnchanged);
    }
}
