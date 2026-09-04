using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface ICurrentUserAccessService {
    Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default);
}
