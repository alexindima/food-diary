using FoodDiary.Domain.Entities.Content;
using FoodDiary.Results;

namespace FoodDiary.Application.Email.Common;

public interface IEmailTemplateAdministrationService {
    Task<Result<EmailTemplate>> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken);
}
