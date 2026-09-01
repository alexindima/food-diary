using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

public interface IProfileOverviewReadService {
    Task<Result<ProfileOverviewModel>> GetAsync(UserId userId, CancellationToken cancellationToken);
}
