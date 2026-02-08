using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Admin;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;

public sealed class UpsertAdminEmailTemplateCommandHandler(
    IEmailTemplateRepository repository)
    : ICommandHandler<UpsertAdminEmailTemplateCommand, Result<AdminEmailTemplateResponse>>
{
    public async Task<Result<AdminEmailTemplateResponse>> Handle(
        UpsertAdminEmailTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var key = NormalizeKey(command.Key);
        var locale = NormalizeLocale(command.Locale);

        var template = await repository.UpsertAsync(
            key,
            locale,
            command.Subject,
            command.HtmlBody,
            command.TextBody,
            command.IsActive,
            cancellationToken);

        var response = new AdminEmailTemplateResponse(
            template.Id,
            template.Key,
            template.Locale,
            template.Subject,
            template.HtmlBody,
            template.TextBody,
            template.IsActive,
            template.CreatedOnUtc,
            template.ModifiedOnUtc);

        return Result.Success(response);
    }

    private static string NormalizeKey(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.ToLowerInvariant();
    }

    private static string NormalizeLocale(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "en";
        }

        var lower = value.Trim().ToLowerInvariant();
        return lower.StartsWith("ru") ? "ru" : "en";
    }
}
