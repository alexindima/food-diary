using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface ICurrentUserAccessService {
    Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default);
}
