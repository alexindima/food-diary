using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClients;

public record GetMyClientsQuery(Guid? UserId) : IQuery<Result<IReadOnlyList<ClientSummaryModel>>>, IUserRequest;
