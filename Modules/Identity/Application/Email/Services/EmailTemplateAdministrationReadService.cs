using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Identity.Email.Services;

public sealed class EmailTemplateAdministrationReadService(IEmailTemplateReadModelRepository repository)
    : IEmailTemplateAdministrationReadService {
    public Task<IReadOnlyList<EmailTemplateRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken) =>
        repository.GetRevisionsAsync(key, locale, cancellationToken);
    public Task<IReadOnlyList<EmailTemplateReadModel>> GetTemplatesAsync(CancellationToken cancellationToken) =>
        repository.GetAllReadModelsAsync(cancellationToken);
}
