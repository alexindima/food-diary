using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class FastingSessionInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FastingSession.Create(UserId.Empty, FastingProtocol.F16_8, 16, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(49)]
    public void Create_WithInvalidDuration_Throws(int hours) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingSession.Create(UserId.New(), FastingProtocol.F16_8, hours, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithValidValues_Succeeds() {
        var userId = UserId.New();
        var startedAt = DateTime.UtcNow;

        var session = FastingSession.Create(userId, FastingProtocol.F18_6, 18, startedAt);

        Assert.Equal(userId, session.UserId);
        Assert.Equal(FastingProtocol.F18_6, session.Protocol);
        Assert.Equal(18, session.PlannedDurationHours);
        Assert.Equal(startedAt, session.StartedAtUtc);
        Assert.False(session.IsCompleted);
        Assert.Null(session.EndedAtUtc);
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
        var endedAt = DateTime.UtcNow.AddHours(16);

        session.End(endedAt);

        Assert.True(session.IsCompleted);
        Assert.Equal(endedAt, session.EndedAtUtc);
    }

    [Fact]
    public void End_WhenAlreadyCompleted_IsIdempotent() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow);
        var endedAt = DateTime.UtcNow.AddHours(16);
        session.End(endedAt);
        var laterEndedAt = DateTime.UtcNow.AddHours(20);

        session.End(laterEndedAt);

        Assert.Equal(endedAt, session.EndedAtUtc);
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

        session.UpdateNotes("Test");

        Assert.Null(session.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateNotes_WithNull_ClearsNotes() {
        var session = FastingSession.Create(
            UserId.New(), FastingProtocol.F16_8, 16, DateTime.UtcNow, notes: "Test");

        session.UpdateNotes(null);

        Assert.Null(session.Notes);
    }

    [Theory]
    [InlineData(FastingProtocol.F16_8, 16)]
    [InlineData(FastingProtocol.F18_6, 18)]
    [InlineData(FastingProtocol.F20_4, 20)]
    [InlineData(FastingProtocol.Custom, 16)]
    public void GetDefaultDuration_ReturnsExpectedHours(FastingProtocol protocol, int expectedHours) {
        Assert.Equal(expectedHours, FastingSession.GetDefaultDuration(protocol));
    }

    [Fact]
    public void Create_WithBoundaryDuration_Succeeds() {
        var session1 = FastingSession.Create(UserId.New(), FastingProtocol.Custom, 1, DateTime.UtcNow);
        var session48 = FastingSession.Create(UserId.New(), FastingProtocol.Custom, 48, DateTime.UtcNow);

        Assert.Equal(1, session1.PlannedDurationHours);
        Assert.Equal(48, session48.PlannedDurationHours);
    }
}
