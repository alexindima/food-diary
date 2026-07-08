using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;

public record GetMyDietologistRelationshipQuery(Guid? UserId)
    : IQuery<Result<DietologistRelationshipModel?>>, IUserRequest;
