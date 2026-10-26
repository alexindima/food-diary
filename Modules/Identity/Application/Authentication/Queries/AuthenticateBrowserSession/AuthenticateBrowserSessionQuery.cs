using System.Text.Json.Serialization;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.AuthenticateBrowserSession;

public sealed record AuthenticateBrowserSessionQuery([property: JsonIgnore] string? Credential) : IQuery<Result<BrowserSessionIdentityModel>> {
    public override string ToString() => "AuthenticateBrowserSessionQuery { Credential = [redacted] }";
}
