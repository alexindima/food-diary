namespace FoodDiary.MailInbox.Application.Abstractions;

/// <summary>Recipient-filtered, lossless export for independent mail consumers.</summary>
public interface IInboundMailExportStore {
    Task<IReadOnlyList<Messages.Models.InboundMailExportEntry>> GetExportPageAsync(
        string recipient, DateTimeOffset? beforeReceivedAtUtc, Guid? beforeId, int limit,
        CancellationToken cancellationToken);

    Task<byte[]?> GetRawMimeAsync(Guid id, string recipient, CancellationToken cancellationToken);
}
