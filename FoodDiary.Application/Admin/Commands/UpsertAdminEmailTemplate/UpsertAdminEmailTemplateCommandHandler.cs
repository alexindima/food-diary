using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;

public sealed class UpsertAdminEmailTemplateCommandHandler(
    IEmailTemplateRepository repository)
    : ICommandHandler<UpsertAdminEmailTemplateCommand, Result<AdminEmailTemplateModel>> {
    public async Task<Result<AdminEmailTemplateModel>> Handle(
        UpsertAdminEmailTemplateCommand command,
        CancellationToken cancellationToken) {
        string key = NormalizeKey(command.Key);
        Result<string> localeResult = StringCodeParser.ParseRequiredLanguage(
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

        var response = new AdminEmailTemplateModel(
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

    private static string NormalizeKey(string value) {
        string trimmed = value?.Trim() ?? string.Empty;
        return trimmed.ToLowerInvariant();
    }
}
