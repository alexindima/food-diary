using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class ClientTaskInvariantTests {
    [Fact]
    public void Create_NormalizesValuesAndStartsOpen() {
        var task = ClientTask.Create(
            UserId.New(),
            UserId.New(),
            "  Add protein  ",
            "  Include breakfast  ",
            DateTime.SpecifyKind(new DateTime(2026, 7, 27), DateTimeKind.Utc));

        Assert.Multiple(
            () => Assert.Equal("Add protein", task.Title),
            () => Assert.Equal("Include breakfast", task.Details),
            () => Assert.Equal(ClientTaskStatus.Open, task.Status));
    }

    [Fact]
    public void CompleteAndReopen_ChangeStatus() {
        var task = ClientTask.Create(
            UserId.New(),
            UserId.New(),
            "Task",
            details: null,
            dueAtUtc: null);

        task.Complete();
        Assert.Equal(ClientTaskStatus.Completed, task.Status);

        task.Reopen();
        Assert.Equal(ClientTaskStatus.Open, task.Status);
    }

    [Fact]
    public void Cancel_BlocksLaterClientChanges() {
        var task = ClientTask.Create(
            UserId.New(),
            UserId.New(),
            "Task",
            details: null,
            dueAtUtc: null);

        task.Cancel();

        Assert.Equal(ClientTaskStatus.Cancelled, task.Status);
        Assert.Throws<InvalidOperationException>(task.Complete);
        Assert.Throws<InvalidOperationException>(task.Reopen);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingTitle_Throws(string title) {
        Assert.Throws<ArgumentException>(() =>
            ClientTask.Create(UserId.New(), UserId.New(), title, details: null, dueAtUtc: null));
    }
}
