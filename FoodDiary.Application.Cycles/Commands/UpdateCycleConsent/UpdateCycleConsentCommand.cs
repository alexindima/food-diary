using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Cycles.Commands.UpdateCycleConsent;

public sealed record UpdateCycleConsentCommand(
    Guid? UserId,
    Guid CycleProfileId,
    int Purpose,
    bool Granted) : ICommand<Result<CycleModel>>, IUserRequest;
