using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Commands.UpdateDietologistPermissions;

public record UpdateDietologistPermissionsCommand(
    Guid? UserId,
    DietologistPermissionsInput Permissions) : ICommand<Result>, IUserRequest;
