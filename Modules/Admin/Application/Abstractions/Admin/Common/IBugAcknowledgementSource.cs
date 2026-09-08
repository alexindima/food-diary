namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IBugAcknowledgementSource {
    IAsyncEnumerable<BugAcknowledgementCandidate> ReadAsync(DateTimeOffset since, CancellationToken cancellationToken);
}

