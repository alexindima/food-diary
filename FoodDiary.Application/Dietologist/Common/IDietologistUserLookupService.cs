using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistUserLookupService {
    Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken);
}
