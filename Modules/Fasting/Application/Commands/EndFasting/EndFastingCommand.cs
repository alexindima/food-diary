using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Commands.EndFasting;

public record EndFastingCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>, IUserRequest;
