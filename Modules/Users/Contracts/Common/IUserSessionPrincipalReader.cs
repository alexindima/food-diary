using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserSessionPrincipalReader {
    Task<Result<UserSessionPrincipalModel>> GetCurrentAsync(UserId userId, DateTime evaluatedAtUtc, CancellationToken cancellationToken = default);
}
