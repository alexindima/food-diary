using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyClients;

public record GetMyClientsQuery(Guid? UserId, int Page = 1, int Limit = 50) : IQuery<Result<IReadOnlyList<ClientSummaryModel>>>, IUserRequest;
