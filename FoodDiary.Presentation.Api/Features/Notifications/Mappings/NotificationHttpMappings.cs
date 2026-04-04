using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Notifications.Queries.GetNotifications;
using FoodDiary.Application.Notifications.Queries.GetUnreadCount;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;

namespace FoodDiary.Presentation.Api.Features.Notifications.Mappings;

public static class NotificationHttpMappings {
    public static GetNotificationsQuery ToNotificationsQuery(this Guid userId) => new(userId);

    public static GetUnreadCountQuery ToUnreadCountQuery(this Guid userId) => new(userId);

    public static MarkNotificationReadCommand ToMarkReadCommand(this Guid notificationId, Guid userId) => new(userId, notificationId);

    public static MarkAllNotificationsReadCommand ToMarkAllReadCommand(this Guid userId) => new(userId);

    public static NotificationHttpResponse ToHttpResponse(this NotificationModel model) =>
        new(model.Id, model.Type, model.Title, model.Body, model.ReferenceId, model.IsRead, model.CreatedAtUtc);
}
