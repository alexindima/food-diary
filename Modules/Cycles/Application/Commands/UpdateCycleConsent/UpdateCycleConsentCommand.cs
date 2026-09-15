using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Commands.UpdateCycleConsent;

public sealed record UpdateCycleConsentCommand(
    Guid? UserId,
    Guid CycleProfileId,
    int Purpose,
    bool Granted) : ICommand<Result<CycleModel>>, IUserRequest;
