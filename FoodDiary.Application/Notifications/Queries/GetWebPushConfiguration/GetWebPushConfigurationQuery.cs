using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushConfiguration;

public sealed record GetWebPushConfigurationQuery : IQuery<Result<WebPushConfigurationModel>>;
