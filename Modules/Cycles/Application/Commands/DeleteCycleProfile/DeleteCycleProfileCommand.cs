using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Cycles.Commands.DeleteCycleProfile;

public sealed record DeleteCycleProfileCommand(Guid? UserId, Guid CycleProfileId) : ICommand<Result>, IUserRequest;
