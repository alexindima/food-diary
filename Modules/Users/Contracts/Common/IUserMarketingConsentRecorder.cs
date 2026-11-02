using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

// Stages the authenticated signup opt-in in the caller's unit of work; never saves.
public interface IUserMarketingConsentRecorder {
    Task<Result> RecordOptInAsync(UserId userId, string noticeVersion, DateTime recordedAtUtc, CancellationToken cancellationToken);
}
