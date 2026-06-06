using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;

public record UpdateDietologistPermissionsCommand(
    Guid? UserId,
    DietologistPermissionsInput Permissions) : ICommand<Result>, IUserRequest;
