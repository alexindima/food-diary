namespace FoodDiary.Application.Abstractions.Email.Common;

public interface IOutgoingEmailJournal {
    Task<OutgoingEmailJournalPage> GetPageAsync(int page, int limit, string? purpose, string? status, string? recipient, CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? id = null, string? correlationId = null);
}
