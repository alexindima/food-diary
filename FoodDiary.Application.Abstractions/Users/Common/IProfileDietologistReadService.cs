using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IProfileDietologistReadService {
    Task<Result<ProfileDietologistRelationshipModel?>> GetRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
