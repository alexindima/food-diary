using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetCurrentFasting;

public record GetCurrentFastingQuery(Guid? UserId) : IQuery<Result<FastingSessionModel?>>, IUserRequest;
