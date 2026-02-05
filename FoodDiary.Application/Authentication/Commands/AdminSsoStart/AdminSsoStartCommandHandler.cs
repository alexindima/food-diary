using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoStart;

public sealed class AdminSsoStartCommandHandler(IAdminSsoService adminSsoService)
    : ICommandHandler<AdminSsoStartCommand, Result<AdminSsoStartResponse>>
{
    public async Task<Result<AdminSsoStartResponse>> Handle(
        AdminSsoStartCommand command,
        CancellationToken cancellationToken)
    {
        var code = await adminSsoService.CreateCodeAsync(command.UserId, cancellationToken);
        var response = new AdminSsoStartResponse(code.Code, code.ExpiresAtUtc);
        return Result.Success(response);
    }
}
