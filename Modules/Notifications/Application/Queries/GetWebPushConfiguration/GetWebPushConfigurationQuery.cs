using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetWebPushConfiguration;

public sealed record GetWebPushConfigurationQuery : IQuery<Result<WebPushConfigurationModel>>;
