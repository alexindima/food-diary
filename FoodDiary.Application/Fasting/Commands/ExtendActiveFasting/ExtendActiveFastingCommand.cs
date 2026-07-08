using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;

public record ExtendActiveFastingCommand(Guid? UserId, int AdditionalHours)
    : ICommand<Result<FastingSessionModel>>, IUserRequest;
