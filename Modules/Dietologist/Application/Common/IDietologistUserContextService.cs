using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistUserContextService : ICurrentUserAccessService {
    Task<Result<string>> GetAccessibleUserEmailAsync(UserId userId, CancellationToken cancellationToken);
    Task<string?> GetUserEmailByIdAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserModel>> GetUserModelByIdAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(UserId userId, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken);
}
