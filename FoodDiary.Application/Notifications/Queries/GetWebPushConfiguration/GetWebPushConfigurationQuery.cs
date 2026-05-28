using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushConfiguration;

public sealed record GetWebPushConfigurationQuery : IQuery<Result<WebPushConfigurationModel>>;
