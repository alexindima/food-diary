using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface ICurrentUserAccessService {
    Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default);
}
