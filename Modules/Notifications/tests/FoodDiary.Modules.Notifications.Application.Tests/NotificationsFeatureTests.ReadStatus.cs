using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Queries.GetNotificationReadStatus;
using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Tests;

public partial class NotificationsFeatureTests {
    [Fact]
    public async Task GetReadStatus_ResolvesOwnerAndDeduplicatesRequestedIdsAsync() {
        User user = CreateUser();
        var id = Guid.NewGuid();
        INotificationReadStatusRepository repository = Substitute.For<INotificationReadStatusRepository>();
        using var cancellation = new CancellationTokenSource();
        repository.GetReadIdsAsync(user.Id, Arg.Any<IReadOnlyCollection<NotificationId>>(), cancellation.Token)
            .Returns(new Guid[] { id });
        var handler = new GetNotificationReadStatusQueryHandler(repository, CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<Guid>> result = await handler.Handle(new GetNotificationReadStatusQuery(user.Id.Value, [id, id]), cancellation.Token);

        ResultAssert.Success(result);
        Assert.Equal(id, Assert.Single(result.Value));
        await repository.Received(1).GetReadIdsAsync(user.Id,
            Arg.Is<IReadOnlyCollection<NotificationId>>(ids => ids.Count == 1 && ids.Single().Value == id), cancellation.Token);
    }

    [Fact]
    public async Task GetReadStatus_RejectsDeletedUserBeforeReadingAsync() {
        INotificationReadStatusRepository repository = Substitute.For<INotificationReadStatusRepository>();
        User user = CreateDeletedUser(UserId.New());
        var handler = new GetNotificationReadStatusQueryHandler(repository, CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<Guid>> result = await handler.Handle(new GetNotificationReadStatusQuery(user.Id.Value, [Guid.NewGuid()]), CancellationToken.None);

        Assert.True(result.IsFailure);
        await repository.DidNotReceiveWithAnyArgs().GetReadIdsAsync(default, [], default);
    }

    [Fact]
    public void GetReadStatus_ValidatesBoundedNonemptyIds() {
        var validator = new GetNotificationReadStatusQueryValidator();
        var userId = Guid.NewGuid();
        Assert.False(validator.Validate(new GetNotificationReadStatusQuery(userId, [])).IsValid);
        Assert.False(validator.Validate(new GetNotificationReadStatusQuery(userId, null!)).IsValid);
        Assert.False(validator.Validate(new GetNotificationReadStatusQuery(userId, [Guid.Empty])).IsValid);
        Assert.False(validator.Validate(new GetNotificationReadStatusQuery(userId, Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray())).IsValid);
        Assert.True(validator.Validate(new GetNotificationReadStatusQuery(userId, Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToArray())).IsValid);
    }
}
