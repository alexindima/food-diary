using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.ExchangeAdminImpersonation;

public sealed class ExchangeAdminImpersonationCommandHandler(IAdminImpersonationHandoffService handoffService)
    : ICommandHandler<ExchangeAdminImpersonationCommand, Result<string>> {
    public async Task<Result<string>> Handle(ExchangeAdminImpersonationCommand command, CancellationToken cancellationToken) {
        string? accessToken = await handoffService.ConsumeCodeAsync(command.Code, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(accessToken)
            ? Result.Failure<string>(Errors.Authentication.InvalidToken)
            : Result.Success(accessToken);
    }
}
