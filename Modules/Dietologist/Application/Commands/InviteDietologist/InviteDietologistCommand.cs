using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Commands.InviteDietologist;

public record InviteDietologistCommand(
    Guid? UserId,
    string DietologistEmail,
    DietologistPermissionsInput Permissions) : ICommand<Result>, IUserRequest;
