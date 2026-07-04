using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IEmailTemplateWriteRepository : IEmailTemplateReadRepository {
    Task<EmailTemplate> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken = default);
}
