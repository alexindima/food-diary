using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoStart;

public sealed class AdminSsoStartCommandHandler(IAdminSsoService adminSsoService)
    : ICommandHandler<AdminSsoStartCommand, Result<AdminSsoStartModel>> {
    public async Task<Result<AdminSsoStartModel>> Handle(
        AdminSsoStartCommand command,
        CancellationToken cancellationToken) {
        var code = await adminSsoService.CreateCodeAsync(new UserId(command.UserId), cancellationToken);
        var response = new AdminSsoStartModel(code.Code, code.ExpiresAtUtc);
        return Result.Success(response);
    }
}
