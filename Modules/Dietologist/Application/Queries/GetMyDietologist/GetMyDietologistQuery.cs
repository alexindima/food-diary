using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyDietologist;

public record GetMyDietologistQuery(Guid? UserId) : IQuery<Result<DietologistInfoModel?>>, IUserRequest;
