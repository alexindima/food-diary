using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;

public record UpdateDietologistPermissionsCommand(
    Guid? UserId,
    DietologistPermissionsInput Permissions) : ICommand<Result>, IUserRequest;
