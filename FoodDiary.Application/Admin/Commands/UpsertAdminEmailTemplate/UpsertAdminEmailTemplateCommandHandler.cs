using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Email.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;

public sealed class UpsertAdminEmailTemplateCommandHandler(
    IEmailTemplateAdministrationService administrationService)
    : ICommandHandler<UpsertAdminEmailTemplateCommand, Result<AdminEmailTemplateModel>> {
    public async Task<Result<AdminEmailTemplateModel>> Handle(
        UpsertAdminEmailTemplateCommand command,
        CancellationToken cancellationToken) {
        string key = NormalizeKey(command.Key);
        Result<string> localeResult = AdminLocaleParser.ParseRequiredLanguage(
            command.Locale,
            nameof(command.Locale),
            "Locale must be one of the supported codes.");
        if (localeResult.IsFailure) {
            return Result.Failure<AdminEmailTemplateModel>(localeResult.Error);
        }

        Result<EmailTemplate> templateResult = await administrationService.UpsertAsync(
            key,
            localeResult.Value,
            command.Subject,
            command.HtmlBody,
            command.TextBody,
            command.IsActive,
            cancellationToken).ConfigureAwait(false);

        return templateResult.IsSuccess
            ? Result.Success(templateResult.Value.ToAdminModel())
            : Result.Failure<AdminEmailTemplateModel>(templateResult.Error);
    }

    private static string NormalizeKey(string value) {
        string trimmed = value.Trim();
        return trimmed.ToLowerInvariant();
    }
}
