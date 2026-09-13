using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IEmailTemplateAdministrationService {
    Task<Result<EmailTemplateReadModel>> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken);
}
