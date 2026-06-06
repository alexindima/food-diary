using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushConfiguration;

public sealed class GetWebPushConfigurationQueryHandler(IWebPushConfigurationProvider webPushConfigurationProvider)
    : IQueryHandler<GetWebPushConfigurationQuery, Result<WebPushConfigurationModel>> {
    public Task<Result<WebPushConfigurationModel>> Handle(
        GetWebPushConfigurationQuery query,
        CancellationToken cancellationToken) {
        WebPushClientConfiguration configuration = webPushConfigurationProvider.GetClientConfiguration();
        return Task.FromResult(Result.Success(new WebPushConfigurationModel(configuration.Enabled, configuration.PublicKey)));
    }
}
