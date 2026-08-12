using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Dietologist.Common;

internal static class ClientTaskMapping {
    public static ClientTaskModel ToModel(this ClientTask task, DateTime utcNow) =>
        new(
            task.Id.Value,
            task.DietologistUserId.Value,
            task.ClientUserId.Value,
            task.Title,
            task.Details,
            task.DueAtUtc,
            task.Status,
            IsOverdue(task.DueAtUtc, task.Status, utcNow),
            task.CreatedOnUtc,
            task.StatusChangedAtUtc);

    public static ClientTaskModel ToModel(this ClientTaskReadModel task, DateTime utcNow) =>
        new(
            task.Id,
            task.DietologistUserId,
            task.ClientUserId,
            task.Title,
            task.Details,
            task.DueAtUtc,
            task.Status,
            IsOverdue(task.DueAtUtc, task.Status, utcNow),
            task.CreatedAtUtc,
            task.StatusChangedAtUtc);

    private static bool IsOverdue(DateTime? dueAtUtc, ClientTaskStatus status, DateTime utcNow) =>
        dueAtUtc < utcNow && status == ClientTaskStatus.Open;
}
