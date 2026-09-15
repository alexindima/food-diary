using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetTelegramConfiguration;

public sealed class GetTelegramConfigurationQueryHandler(ITelegramIdentityPolicy policy, ITelegramOidcProvider oidc)
    : IQueryHandler<GetTelegramConfigurationQuery, Result<TelegramConfigurationModel>> {
    public Task<Result<TelegramConfigurationModel>> Handle(GetTelegramConfigurationQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new TelegramConfigurationModel(policy.LoginEnabled, policy.LoginEnabled && policy.RegistrationEnabled, policy.LoginEnabled && oidc.IsEnabled)));
}
