using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;

public interface IEmailTemplateReadModelRepository {
    Task<IReadOnlyList<EmailTemplateRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default);
}
