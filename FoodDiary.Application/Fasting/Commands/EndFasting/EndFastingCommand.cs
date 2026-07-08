using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.EndFasting;

public record EndFastingCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>, IUserRequest;
