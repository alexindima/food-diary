using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetWebPushConfiguration;

public sealed class GetWebPushConfigurationQueryHandler(IWebPushConfigurationProvider webPushConfigurationProvider)
    : IQueryHandler<GetWebPushConfigurationQuery, Result<WebPushConfigurationModel>> {
    public Task<Result<WebPushConfigurationModel>> Handle(
        GetWebPushConfigurationQuery query,
        CancellationToken cancellationToken) {
        WebPushClientConfiguration configuration = webPushConfigurationProvider.GetClientConfiguration();
        return Task.FromResult(Result.Success(new WebPushConfigurationModel(configuration.Enabled, configuration.PublicKey)));
    }
}
