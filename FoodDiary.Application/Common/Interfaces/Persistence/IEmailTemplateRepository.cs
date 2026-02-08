using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IEmailTemplateRepository
{
    Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetByKeyAsync(string key, string locale, CancellationToken cancellationToken = default);
    Task<EmailTemplate> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken = default);
}
