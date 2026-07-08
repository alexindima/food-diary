using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClients;

public record GetMyClientsQuery(Guid? UserId) : IQuery<Result<IReadOnlyList<ClientSummaryModel>>>, IUserRequest;
