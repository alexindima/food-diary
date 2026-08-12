using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushConfiguration;

public sealed record GetWebPushConfigurationQuery : IQuery<Result<WebPushConfigurationModel>>;
