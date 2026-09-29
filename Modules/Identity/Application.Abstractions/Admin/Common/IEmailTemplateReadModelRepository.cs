using FoodDiary.Modules.Identity.Contracts.Admin.Models;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;

public interface IEmailTemplateReadModelRepository {
    Task<IReadOnlyList<EmailTemplateRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default);
    async Task<IReadOnlyList<EmailTemplateReadModel>> GetPageReadModelsAsync(
        int page,
        int limit,
        CancellationToken cancellationToken = default) =>
        (await GetAllReadModelsAsync(cancellationToken).ConfigureAwait(false)).Skip((page - 1) * limit).Take(limit).ToList();
}
