using FoodDiary.Application.Notifications.Common;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace FoodDiary.Presentation.Api.Features.Notifications;

[ApiController]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController(
    ISender mediator,
    IHostEnvironment environment,
    INotificationTextRenderer notificationTextRenderer)
    : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<NotificationHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetNotifications([FromCurrentUser] Guid userId) {
        if (environment.IsDevelopment()) {
            return Task.FromResult<IActionResult>(Ok(GetFakeNotifications(GetPreferredLocale())));
        }

        return HandleOk(userId.ToNotificationsQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());
    }

    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCountHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetUnreadCount([FromCurrentUser] Guid userId) {
        if (environment.IsDevelopment()) {
            return Task.FromResult<IActionResult>(Ok(new UnreadCountHttpResponse(2)));
        }

        return HandleOk(userId.ToUnreadCountQuery(), static value => new UnreadCountHttpResponse(value));
    }

    [HttpPut("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarkAsRead(Guid notificationId, [FromCurrentUser] Guid userId) {
        if (environment.IsDevelopment()) {
            return Task.FromResult<IActionResult>(NoContent());
        }

        return HandleNoContent(notificationId.ToMarkReadCommand(userId));
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> MarkAllAsRead([FromCurrentUser] Guid userId) {
        if (environment.IsDevelopment()) {
            return Task.FromResult<IActionResult>(NoContent());
        }

        return HandleNoContent(userId.ToMarkAllReadCommand());
    }

    private List<NotificationHttpResponse> GetFakeNotifications(string locale) {
        var now = DateTime.UtcNow;

        return [
            CreateFakeNotification(Guid.Parse("11111111-1111-1111-1111-111111111111"), NotificationTypes.FastingCompleted, "fasting-session-1", false, now.AddMinutes(-8), locale),
            CreateFakeNotification(Guid.Parse("22222222-2222-2222-2222-222222222222"), NotificationTypes.WeeklyCheckIn, "weekly-check-in", false, now.AddHours(-2), locale),
            CreateFakeNotification(Guid.Parse("33333333-3333-3333-3333-333333333333"), NotificationTypes.Hydration, "hydration", true, now.AddHours(-5), locale),
            CreateFakeNotification(Guid.Parse("44444444-4444-4444-4444-444444444444"), NotificationTypes.GoalReached, "goals", true, now.AddHours(-9), locale),
            CreateFakeNotification(Guid.Parse("55555555-5555-5555-5555-555555555555"), NotificationTypes.Lesson, "lessons", true, now.AddDays(-1), locale),
            CreateFakeNotification(Guid.Parse("66666666-6666-6666-6666-666666666666"), NotificationTypes.MealPlan, "meal-plans", true, now.AddDays(-2), locale),
            CreateFakeNotification(Guid.Parse("77777777-7777-7777-7777-777777777777"), NotificationTypes.Achievement, "gamification", true, now.AddDays(-4), locale),
        ];
    }

    private NotificationHttpResponse CreateFakeNotification(
        Guid id,
        string type,
        string referenceId,
        bool isRead,
        DateTime createdAtUtc,
        string locale) {
        var notificationText = notificationTextRenderer.RenderFromPayload(type, NotificationPayloads.Empty(), locale);
        return new NotificationHttpResponse(id, type, notificationText.Title, notificationText.Body, referenceId, isRead, createdAtUtc);
    }

    private string GetPreferredLocale() {
        var header = Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header)) {
            return "en";
        }

        return header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "en";
    }
}
