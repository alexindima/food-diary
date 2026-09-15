using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyDietologistRelationship;

public record GetMyDietologistRelationshipQuery(Guid? UserId)
    : IQuery<Result<DietologistRelationshipModel?>>, IUserRequest;
