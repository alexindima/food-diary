using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyClients;

public record GetMyClientsQuery(Guid? UserId) : IQuery<Result<IReadOnlyList<ClientSummaryModel>>>, IUserRequest;
