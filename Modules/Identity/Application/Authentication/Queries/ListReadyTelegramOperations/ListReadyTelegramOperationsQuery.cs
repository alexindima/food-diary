using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Queries.ListReadyTelegramOperations;

public sealed record ListReadyTelegramOperationsQuery : IQuery<Result<IReadOnlyList<Guid>>>;
