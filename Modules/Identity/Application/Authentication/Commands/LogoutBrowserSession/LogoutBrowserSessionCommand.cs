using System.Text.Json.Serialization;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.LogoutBrowserSession;

public sealed record LogoutBrowserSessionCommand([property: JsonIgnore] string? Credential) : ICommand<Result> {
    public override string ToString() => "LogoutBrowserSessionCommand { Credential = [redacted] }";
}
