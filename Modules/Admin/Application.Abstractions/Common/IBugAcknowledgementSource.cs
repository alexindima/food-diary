namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IBugAcknowledgementSource {
    IAsyncEnumerable<BugAcknowledgementCandidate> ReadAsync(DateTimeOffset since, CancellationToken cancellationToken);
}

