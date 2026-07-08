using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetCurrentFasting;

public record GetCurrentFastingQuery(Guid? UserId) : IQuery<Result<FastingSessionModel?>>, IUserRequest;
