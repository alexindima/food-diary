using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Common;

public interface IDietologistUserContextService : ICurrentUserAccessService {
    Task<Result<string>> GetAccessibleUserEmailAsync(UserId userId, CancellationToken cancellationToken);
    Task<string?> GetUserEmailByIdAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserModel>> GetUserModelByIdAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(UserId userId, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken);
}
