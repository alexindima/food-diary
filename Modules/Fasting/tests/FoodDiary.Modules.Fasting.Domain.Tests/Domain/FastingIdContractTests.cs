using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class FastingIdContractTests {
    [Fact]
    public void FastingCheckInId_ImplementsGuidValueObjectContract() {
        AssertContract(
            static value => (FastingCheckInId)value,
            static id => id,
            FastingCheckInId.Empty);
    }

    [Fact]
    public void FastingOccurrenceId_ImplementsGuidValueObjectContract() {
        AssertContract(
            static value => (FastingOccurrenceId)value,
            static id => id,
            FastingOccurrenceId.Empty);
    }

    [Fact]
    public void FastingPlanId_ImplementsGuidValueObjectContract() {
        AssertContract(
            static value => (FastingPlanId)value,
            static id => id,
            FastingPlanId.Empty);
    }

    [Fact]
    public void FastingSessionId_ImplementsGuidValueObjectContract() {
        AssertContract(
            static value => (FastingSessionId)value,
            static id => id,
            FastingSessionId.Empty);
    }

    [Fact]
    public void FastingTelemetryEventId_ImplementsGuidValueObjectContract() {
        AssertContract(
            static value => (FastingTelemetryEventId)value,
            static id => id,
            FastingTelemetryEventId.Empty);
    }

    private static void AssertContract<TId>(
        Func<Guid, TId> fromGuid,
        Func<TId, Guid> toGuid,
        TId empty)
        where TId : struct {
        var value = new Guid("cf54b7a8-6d17-4f81-b0ca-50be71393c69");
        TId id = fromGuid(value);

        Assert.Multiple(
            () => Assert.Equal(value, toGuid(id)),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, toGuid(empty)));
    }
}
