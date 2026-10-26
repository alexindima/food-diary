using System.Text.Json.Serialization;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CreateBrowserSession;

public sealed record CreateBrowserSessionCommand(
    [property: JsonIgnore] string RefreshToken,
    [property: JsonIgnore] string? ExistingCredential = null,
    DateTime? MaximumExpiresAtUtc = null) : ICommand<Result<BrowserSessionModel>> {
    public override string ToString() => "CreateBrowserSessionCommand { Credentials = [redacted] }";
}
