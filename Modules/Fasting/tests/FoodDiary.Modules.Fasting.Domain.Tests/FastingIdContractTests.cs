using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingIdContractTests {
    [Fact]
    public void ReplacementIds_LeaveEveryPublishedOriginalUnchanged() {
        var value = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacement = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var checkIn = new FastingCheckInId(value);
        var occurrence = new FastingOccurrenceId(value);
        var plan = new FastingPlanId(value);
        var session = new FastingSessionId(value);
        var telemetry = new FastingTelemetryEventId(value);

        Assert.Multiple(
            () => Assert.Equal(replacement, (checkIn with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (occurrence with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (plan with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (session with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (telemetry with { Value = replacement }).Value),
            () => Assert.Equal(value, checkIn.Value),
            () => Assert.Equal(value, occurrence.Value),
            () => Assert.Equal(value, plan.Value),
            () => Assert.Equal(value, session.Value),
            () => Assert.Equal(value, telemetry.Value));
    }

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
