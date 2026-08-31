using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class AdminInvariantTests {
    [Fact]
    public void AdminImpersonationSession_Start_WithValidValues_NormalizesFieldsAndTimestamp() {
        var actorUserId = UserId.New();
        var targetUserId = UserId.New();
        var startedAtLocal = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Local);

        var session = AdminImpersonationSession.Start(
            actorUserId,
            targetUserId,
            reason: "  Support investigation  ",
            actorIpAddress: "  127.0.0.1  ",
            actorUserAgent: "  Browser  ",
            startedAtLocal);

        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, session.Id),
            () => Assert.Equal(actorUserId, session.ActorUserId),
            () => Assert.Equal(targetUserId, session.TargetUserId),
            () => Assert.Equal("Support investigation", session.Reason),
            () => Assert.Equal("127.0.0.1", session.ActorIpAddress),
            () => Assert.Equal("Browser", session.ActorUserAgent),
            () => Assert.Equal(startedAtLocal.ToUniversalTime(), session.StartedAtUtc),
            () => Assert.Equal(startedAtLocal.ToUniversalTime(), session.CreatedOnUtc));
    }

    [Fact]
    public void AdminImpersonationSession_Start_WithBlankOptionalText_StoresNulls() {
        var session = AdminImpersonationSession.Start(
            UserId.New(),
            UserId.New(),
            reason: "Support case",
            actorIpAddress: " ",
            actorUserAgent: " ",
            DateTime.UtcNow);

        Assert.Null(session.ActorIpAddress);
        Assert.Null(session.ActorUserAgent);
    }

    [Fact]
    public void AdminImpersonationSession_Start_WithLongOptionalText_TruncatesValues() {
        var session = AdminImpersonationSession.Start(
            UserId.New(),
            UserId.New(),
            reason: "Support case",
            actorIpAddress: new string('i', 129),
            actorUserAgent: new string('u', 513),
            DateTime.UtcNow);

        Assert.Equal(128, session.ActorIpAddress!.Length);
        Assert.Equal(512, session.ActorUserAgent!.Length);
    }

    [Fact]
    public void AdminImpersonationSession_Start_WithEmptyIds_Throws() {
        Assert.Throws<ArgumentException>(() =>
            AdminImpersonationSession.Start(UserId.Empty, UserId.New(), "Support case", actorIpAddress: null, actorUserAgent: null, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            AdminImpersonationSession.Start(UserId.New(), UserId.Empty, "Support case", actorIpAddress: null, actorUserAgent: null, DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("too short")]
    public void AdminImpersonationSession_Start_WithInvalidReason_Throws(string reason) {
        Assert.ThrowsAny<ArgumentException>(() =>
            AdminImpersonationSession.Start(UserId.New(), UserId.New(), reason, actorIpAddress: null, actorUserAgent: null, DateTime.UtcNow));
    }

    [Fact]
    public void AdminImpersonationSession_Start_WithTooLongReason_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AdminImpersonationSession.Start(UserId.New(), UserId.New(), new string('r', 501), actorIpAddress: null, actorUserAgent: null, DateTime.UtcNow));
    }

    [Fact]
    public void AdminImpersonationSession_Start_WithUnspecifiedTimestamp_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AdminImpersonationSession.Start(
                UserId.New(),
                UserId.New(),
                "Support case",
                actorIpAddress: null,
                actorUserAgent: null,
                new DateTime(2026, 3, 27)));
    }
}
