using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationFromOperation;

public sealed record CreateHydrationFromOperationCommand(Guid? UserId, Guid OperationId, DateTime TimestampUtc, int AmountMl)
    : ICommand<Result<HydrationOperationModel>>, IUserRequest;
