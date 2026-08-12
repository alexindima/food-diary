using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserDietologistProfileReadService {
    Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(UserId userId, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}
