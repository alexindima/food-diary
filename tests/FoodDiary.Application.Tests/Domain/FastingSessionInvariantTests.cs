using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class FastingSessionInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FastingSession.Create(UserId.Empty, FastingProtocol.F16_8, 16, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(169)]
    public void Create_WithInvalidDuration_Throws(int hours) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingSession.Create(UserId.New(), FastingProtocol.F16_8, hours, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithValidValues_Succeeds() {
        var userId = UserId.New();
        DateTime startedAt = DateTime.UtcNow;

        var session = FastingSession.Create(userId, FastingProtocol.F18_6, 18, startedAt);

        Assert.Multiple(
            () => Assert.Equal(userId, session.UserId),
            () => Assert.Equal(FastingProtocol.F18_6, session.Protocol),
            () => Assert.Equal(18, session.InitialPlannedDurationHours),
            () => Assert.Equal(0, session.AddedDurationHours),
            () => Assert.Equal(18, session.PlannedDurationHours),
            () => Assert.Equal(startedAt, session.StartedAtUtc),
            () => Assert.False(session.IsCompleted),
            () => Assert.Null(session.EndedAtUtc));
    }

    [Fact]
    public void Create_WithNotes_TrimsNotes() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow, notes: "  Feeling good  ");

        Assert.Equal("Feeling good", session.Notes);
    }

    [Fact]
    public void Create_WithTooLongNotes_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingSession.Create(
                UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow,
                notes: new string('n', 501)));
    }

    [Fact]
    public void Create_WithWhitespaceNotes_SetsNull() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow, notes: "   ");

        Assert.Null(session.Notes);
    }

    [Fact]
    public void End_SetsCompletedAndEndedAt() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow);
        DateTime endedAt = DateTime.UtcNow.AddHours(16);

        session.End(endedAt);

        Assert.Multiple(
            () => Assert.True(session.IsCompleted),
            () => Assert.Equal(endedAt, session.EndedAtUtc),
            () => Assert.Equal(FastingSessionStatus.Completed, session.Status));
    }

    [Theory]
    [InlineData(FastingProtocol.F16_8, 16)]
    [InlineData(FastingProtocol.F18_6, 18)]
    [InlineData(FastingProtocol.F20_4, 20)]
    [InlineData(FastingProtocol.CustomIntermittent, 16)]
    public void End_IntermittentBeforeTargetReached_SetsCompletedStatus(FastingProtocol protocol, int plannedDurationHours) {
        DateTime startedAt = DateTime.UtcNow;
        var session = FastingSession.Create(
            UserId.New(), protocol, plannedDurationHours, startedAt);

        session.End(startedAt.AddHours(10));

        Assert.Multiple(
            () => Assert.True(session.IsCompleted),
            () => Assert.Equal(FastingSessionStatus.Completed, session.Status),
            () => Assert.True(session.IsSuccessfulCompletion));
    }

    [Fact]
    public void End_ExtendedBeforeTargetReached_SetsInterruptedStatus() {
        DateTime startedAt = DateTime.UtcNow;
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F72_0, 72, startedAt);

        session.End(startedAt.AddHours(24));

        Assert.Multiple(
            () => Assert.True(session.IsCompleted),
            () => Assert.Equal(FastingSessionStatus.Interrupted, session.Status),
            () => Assert.False(session.IsSuccessfulCompletion));
    }

    [Fact]
    public void End_NonIntermittentAfterTargetReached_SetsCompletedStatus() {
        DateTime startedAt = DateTime.UtcNow;
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F24_0, 24, startedAt);

        session.End(startedAt.AddHours(24));

        Assert.Equal(FastingSessionStatus.Completed, session.Status);
        Assert.True(session.IsSuccessfulCompletion);
    }

    [Fact]
    public void GetStatus_WhenSessionHasNotEnded_ReturnsActive() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow);

        Assert.Equal(FastingSessionStatus.Active, session.GetStatus());
    }

    [Theory]
    [InlineData(FastingProtocol.F24_0, 24, 23, FastingSessionStatus.Interrupted)]
    [InlineData(FastingProtocol.F36_0, 36, 36, FastingSessionStatus.Completed)]
    [InlineData(FastingProtocol.F72_0, 72, 73, FastingSessionStatus.Completed)]
    public void GetStatus_ForExtendedProtocols_UsesPlannedDurationTarget(
        FastingProtocol protocol,
        int plannedDurationHours,
        int elapsedHours,
        FastingSessionStatus expectedStatus) {
        DateTime startedAt = DateTime.UtcNow;
        var session = FastingSession.Create(
            UserId.New(), protocol, plannedDurationHours, startedAt);
        session.End(startedAt.AddHours(elapsedHours));

        Assert.Equal(expectedStatus, session.GetStatus());
    }

    [Fact]
    public void End_WhenAlreadyCompleted_IsIdempotent() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow);
        DateTime endedAt = DateTime.UtcNow.AddHours(16);
        session.End(endedAt);
        DateTime? modifiedOnUtc = session.ModifiedOnUtc;
        DateTime laterEndedAt = DateTime.UtcNow.AddHours(20);

        session.End(laterEndedAt);

        Assert.Equal(endedAt, session.EndedAtUtc);
        Assert.Equal(modifiedOnUtc, session.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateNotes_WithNewValue_SetsModifiedOnUtc() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow);

        session.UpdateNotes("New notes");

        Assert.Equal("New notes", session.Notes);
        Assert.NotNull(session.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateNotes_WithSameValue_DoesNotSetModifiedOnUtc() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow, notes: "Test");

        session.UpdateNotes("  Test  ");

        Assert.Null(session.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateNotes_WithNull_ClearsNotes() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow, notes: "Test");

        session.UpdateNotes(notes: null);

        Assert.Null(session.Notes);
    }

    [Theory]
    [InlineData(FastingProtocol.F16_8, 16)]
    [InlineData(FastingProtocol.F18_6, 18)]
    [InlineData(FastingProtocol.F20_4, 20)]
    [InlineData(FastingProtocol.CustomIntermittent, 16)]
    [InlineData(FastingProtocol.F24_0, 24)]
    [InlineData(FastingProtocol.F36_0, 36)]
    [InlineData(FastingProtocol.F72_0, 72)]
    [InlineData(FastingProtocol.Custom, 16)]
    public void GetDefaultDuration_ReturnsExpectedHours(FastingProtocol protocol, int expectedHours) {
        Assert.Equal(expectedHours, FastingSession.GetDefaultDuration(protocol));
    }

    [Fact]
    public void Create_WithBoundaryDuration_Succeeds() {
        var session1 = FastingSession.Create(UserId.New(), FastingProtocol.Custom, 1, DateTime.UtcNow);
        var session168 = FastingSession.Create(UserId.New(), FastingProtocol.Custom, 168, DateTime.UtcNow);

        Assert.Equal(1, session1.PlannedDurationHours);
        Assert.Equal(168, session168.PlannedDurationHours);
    }

    [Fact]
    public void Extend_WithValidHours_IncreasesPlannedDuration() {
        var session = FastingSession.Create(UserId.New(), FastingProtocol.F72_0, 72, DateTime.UtcNow);

        session.Extend(24);

        Assert.Multiple(
            () => Assert.Equal(72, session.InitialPlannedDurationHours),
            () => Assert.Equal(24, session.AddedDurationHours),
            () => Assert.Equal(96, session.PlannedDurationHours));
        Assert.NotNull(session.ModifiedOnUtc);
    }

    [Fact]
    public void Extend_WhenCompleted_Throws() {
        DateTime startedAt = DateTime.UtcNow;
        var session = FastingSession.Create(UserId.New(), FastingProtocol.F16_8, 16, startedAt);
        session.End(startedAt.AddHours(16));

        Assert.Throws<InvalidOperationException>(() => session.Extend(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(97)]
    public void Extend_WithInvalidHours_Throws(int additionalHours) {
        var session = FastingSession.Create(UserId.New(), FastingProtocol.F72_0, 72, DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Extend(additionalHours));
    }

    [Fact]
    public void GetDefaultDuration_WithUnknownProtocol_ReturnsDefaultDuration() {
        Assert.Equal(16, FastingSession.GetDefaultDuration((FastingProtocol)999));
    }
}
