using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserDietologistProfileReadService {
    Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(UserId userId, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}
