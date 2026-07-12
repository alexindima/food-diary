using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Common;

public interface IProfileDietologistReadService {
    Task<Result<ProfileDietologistRelationshipModel?>> GetRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
