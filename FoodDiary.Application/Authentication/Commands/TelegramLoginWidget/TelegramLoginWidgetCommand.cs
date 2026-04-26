using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;

public sealed record TelegramLoginWidgetCommand(
    long Id,
    long AuthDate,
    string Hash,
    string? Username,
    string? FirstName,
    string? LastName,
    string? PhotoUrl) : ICommand<Result<AuthenticationModel>>;
