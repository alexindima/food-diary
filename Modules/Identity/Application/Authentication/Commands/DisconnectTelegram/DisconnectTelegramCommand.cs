using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;
using System.Text.Json.Serialization;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.DisconnectTelegram;

public sealed record DisconnectTelegramCommand(
    Guid? UserId,
    long? SecurityVersion,
    [property: JsonIgnore] string? Password,
    [property: JsonIgnore] string? GoogleCredential,
    AuthenticationClientContext ClientContext,
    bool RememberMe) : ICommand<Result<AuthenticationModel>>, IUserRequest {
    public override string ToString() => nameof(DisconnectTelegramCommand);
}
