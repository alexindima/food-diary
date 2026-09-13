using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Email.Services;

public sealed class EmailTemplateAdministrationService(IEmailTemplateWriteRepository repository)
    : IEmailTemplateAdministrationService {
    public async Task<Result<EmailTemplateReadModel>> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken) {
        EmailTemplate template = await repository.UpsertAsync(
            key,
            locale,
            subject,
            htmlBody,
            textBody,
            isActive,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new EmailTemplateReadModel(
            template.Id, template.Key, template.Locale, template.Subject, template.HtmlBody,
            template.TextBody, template.IsActive, template.CreatedOnUtc, template.ModifiedOnUtc));
    }
}
