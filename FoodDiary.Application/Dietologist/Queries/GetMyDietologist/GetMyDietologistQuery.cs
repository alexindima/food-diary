using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologist;

public record GetMyDietologistQuery(Guid? UserId) : IQuery<Result<DietologistInfoModel?>>, IUserRequest;
