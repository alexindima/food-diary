using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ClientTaskTimestampTests {
    private static readonly DateTime Now = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ClientTask_RejectsUnspecifiedDomainTimestamps() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ClientTask.Create(
            UserId.New(),
            UserId.New(),
            "Task",
            details: null,
            new DateTime(2026, 8, 20)));

        var task = ClientTask.Create(
            UserId.New(),
            UserId.New(),
            "Task",
            details: null,
            Now.AddDays(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => task.MarkDueReminderSent(new DateTime(2026, 8, 20)));
        Assert.Null(task.DueReminderSentAtUtc);
    }
}
