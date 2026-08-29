using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Commands.ExtendActiveFasting;

public record ExtendActiveFastingCommand(Guid? UserId, int AdditionalHours)
    : ICommand<Result<FastingSessionModel>>, IUserRequest;
