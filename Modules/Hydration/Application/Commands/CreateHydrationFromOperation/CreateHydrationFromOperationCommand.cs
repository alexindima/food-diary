using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Hydration.Application.Commands.CreateHydrationFromOperation;

public sealed record CreateHydrationFromOperationCommand(Guid? UserId, Guid OperationId, DateTime TimestampUtc, int AmountMl)
    : ICommand<Result<HydrationOperationModel>>, IUserRequest;
