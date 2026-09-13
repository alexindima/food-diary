using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IEmailTemplateAdministrationReadService {
    Task<IReadOnlyList<EmailTemplateRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailTemplateReadModel>> GetTemplatesAsync(CancellationToken cancellationToken);
}
