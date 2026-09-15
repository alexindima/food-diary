using FoodDiary.Modules.Identity.Domain.Entities.Content;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;

public interface IEmailTemplateWriteRepository {
    Task<EmailTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default);

    Task<EmailTemplate> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken = default);
}
