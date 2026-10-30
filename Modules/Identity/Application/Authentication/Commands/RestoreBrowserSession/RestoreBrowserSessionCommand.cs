using System.Text.Json.Serialization;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RestoreBrowserSession;

public sealed record RestoreBrowserSessionCommand([property: JsonIgnore] string? Credential, bool RememberMe = false) : ICommand<Result<BrowserSessionModel>> {
    public override string ToString() => "RestoreBrowserSessionCommand { Credential = [redacted] }";
}
