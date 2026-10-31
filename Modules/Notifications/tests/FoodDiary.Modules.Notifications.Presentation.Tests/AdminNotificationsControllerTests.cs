using FoodDiary.Mediator;
using FoodDiary.Modules.Notifications.Application.Commands.ScheduleAdminTestNotification;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Modules.Notifications.Presentation.Controllers;
using FoodDiary.Modules.Notifications.Presentation.Requests;
using FoodDiary.Modules.Notifications.Presentation.Responses;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Notifications.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminNotificationsControllerTests {
    [Fact]
    public async Task ScheduleTestNotification_UsesRouteTargetAndCurrentActorAsync() {
        var actor = Guid.NewGuid();
        var target = Guid.NewGuid();
        DateTime scheduledAt = DateTime.UtcNow.AddSeconds(15);
        IRequest<Result<ScheduledNotificationModel>>? sent = null;
        ISender sender = SubstituteSender.Create(
            Result.Success(new ScheduledNotificationModel("FastingCompleted", 15, scheduledAt)), request => sent = request);
        var controller = new AdminNotificationsController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        IActionResult result = await controller.ScheduleTestNotification(target, actor, new ScheduleTestNotificationHttpRequest(15, "FastingCompleted"));

        AcceptedResult accepted = Assert.IsType<AcceptedResult>(result);
        ScheduledNotificationHttpResponse response = Assert.IsType<ScheduledNotificationHttpResponse>(accepted.Value);
        ScheduleAdminTestNotificationCommand command = Assert.IsType<ScheduleAdminTestNotificationCommand>(sent);
        Assert.Multiple(
            () => Assert.Equal(target, command.TargetUserId),
            () => Assert.Equal(actor, command.ActorUserId),
            () => Assert.Equal("FastingCompleted", command.Type),
            () => Assert.Equal(15, command.DelaySeconds),
            () => Assert.Equal(scheduledAt, response.ScheduledAtUtc));
    }
}
