using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetTelegramConfiguration;

public sealed record GetTelegramConfigurationQuery : IQuery<Result<TelegramConfigurationModel>>;
