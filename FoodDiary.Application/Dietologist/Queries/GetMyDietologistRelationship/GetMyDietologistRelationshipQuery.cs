using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;

public record GetMyDietologistRelationshipQuery(Guid? UserId)
    : IQuery<Result<DietologistRelationshipModel?>>, IUserRequest;
