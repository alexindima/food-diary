using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Commands.DeleteCycleProfile;

public sealed record DeleteCycleProfileCommand(Guid? UserId, Guid CycleProfileId) : ICommand<Result>, IUserRequest;
