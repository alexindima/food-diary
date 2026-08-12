using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

internal sealed class DietologistUserLookupService(IUserDietologistProfileReadService profileReadService) : IDietologistUserLookupService {
    public Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        profileReadService.FindByEmailAsync(email, cancellationToken);

    public Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        profileReadService.FindByIdAsync(userId, cancellationToken);
}
