using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IEmailTemplateReadRepository {
    Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<EmailTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) {
        IReadOnlyList<EmailTemplate> templates = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return [.. templates.Select(ToReadModel)];
    }

    Task<EmailTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default);

    private static EmailTemplateReadModel ToReadModel(EmailTemplate template) =>
        new(
            template.Id,
            template.Key,
            template.Locale,
            template.Subject,
            template.HtmlBody,
            template.TextBody,
            template.IsActive,
            template.CreatedOnUtc,
            template.ModifiedOnUtc);
}
