using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;

public sealed class UpsertAdminEmailTemplateCommandHandler(
    IEmailTemplateWriteRepository repository)
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

        EmailTemplate template = await repository.UpsertAsync(
            key,
            localeResult.Value,
            command.Subject,
            command.HtmlBody,
            command.TextBody,
            command.IsActive,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(template.ToAdminModel());
    }

    private static string NormalizeKey(string value) {
        string trimmed = value.Trim();
        return trimmed.ToLowerInvariant();
    }
}
