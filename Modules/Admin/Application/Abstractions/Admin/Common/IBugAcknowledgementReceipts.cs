namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IBugAcknowledgementReceipts {
    Task<bool> ContainsAsync(Guid inboxId, CancellationToken cancellationToken);
    Task RecordAsync(Guid inboxId, CancellationToken cancellationToken);
}
