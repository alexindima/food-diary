using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.RequestTelegramBackupEmail;

public sealed record RequestTelegramBackupEmailCommand(Guid? UserId, string Email, string InitData) : ICommand<Result>, IUserRequest;
