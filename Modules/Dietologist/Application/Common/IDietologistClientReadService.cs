using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistClientReadService {
    Task<Result<UserModel>> GetGoalsAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        CancellationToken cancellationToken);
}
