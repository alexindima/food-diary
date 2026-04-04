using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Commands.InviteDietologist;

public record InviteDietologistCommand(
    Guid? UserId,
    string DietologistEmail,
    DietologistPermissionsInput Permissions) : ICommand<Result>, IUserRequest;
