using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Dietologist;

public sealed class ClientTask : Entity<ClientTaskId> {
    private const int TitleMaxLength = 200;
    private const int DetailsMaxLength = 2000;

    public UserId DietologistUserId { get; private set; }
    public UserId ClientUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public DateTime? DueAtUtc { get; private set; }
    public ClientTaskStatus Status { get; private set; }
    public DateTime? StatusChangedAtUtc { get; private set; }
    public DateTime? DueReminderSentAtUtc { get; private set; }

    private ClientTask() {
    }

    public static ClientTask Create(
        UserId dietologistUserId,
        UserId clientUserId,
        string title,
        string? details,
        DateTime? dueAtUtc) {
        if (dietologistUserId == UserId.Empty) {
            throw new ArgumentException("Dietologist id is required.", nameof(dietologistUserId));
        }

        if (clientUserId == UserId.Empty) {
            throw new ArgumentException("Client id is required.", nameof(clientUserId));
        }

        string normalizedTitle = NormalizeRequired(title, TitleMaxLength, nameof(title));
        string? normalizedDetails = NormalizeOptional(details, DetailsMaxLength, nameof(details));
        var task = new ClientTask {
            Id = ClientTaskId.New(),
            DietologistUserId = dietologistUserId,
            ClientUserId = clientUserId,
            Title = normalizedTitle,
            Details = normalizedDetails,
            DueAtUtc = dueAtUtc?.ToUniversalTime(),
            Status = ClientTaskStatus.Open,
        };
        task.SetCreated();
        return task;
    }

    public void Complete() => ChangeClientStatus(ClientTaskStatus.Completed);

    public void Reopen() => ChangeClientStatus(ClientTaskStatus.Open);

    public void Cancel() {
        if (Status == ClientTaskStatus.Cancelled) {
            return;
        }

        Status = ClientTaskStatus.Cancelled;
        StatusChangedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    public void MarkDueReminderSent(DateTime sentAtUtc) {
        if (DueReminderSentAtUtc is not null) {
            return;
        }

        DueReminderSentAtUtc = sentAtUtc.ToUniversalTime();
        SetModified();
    }

    private void ChangeClientStatus(ClientTaskStatus status) {
        if (Status == ClientTaskStatus.Cancelled) {
            throw new InvalidOperationException("A cancelled task cannot be changed by the client.");
        }

        if (Status == status) {
            return;
        }

        Status = status;
        StatusChangedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Task title is required.", paramName);
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, maxLength, "Value is too long.")
            : normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, maxLength, "Value is too long.")
            : normalized;
    }
}
