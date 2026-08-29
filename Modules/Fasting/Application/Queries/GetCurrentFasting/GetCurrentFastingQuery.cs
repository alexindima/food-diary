using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetCurrentFasting;

public record GetCurrentFastingQuery(Guid? UserId) : IQuery<Result<FastingSessionModel?>>, IUserRequest;
