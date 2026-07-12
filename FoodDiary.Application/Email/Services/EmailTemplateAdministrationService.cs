using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Email.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Results;

namespace FoodDiary.Application.Email.Services;

public sealed class EmailTemplateAdministrationService(IEmailTemplateWriteRepository repository)
    : IEmailTemplateAdministrationService {
    public async Task<Result<EmailTemplate>> UpsertAsync(
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

        return Result.Success(template);
    }
}
