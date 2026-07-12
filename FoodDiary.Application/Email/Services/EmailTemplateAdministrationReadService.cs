using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Email.Common;

namespace FoodDiary.Application.Email.Services;

public sealed class EmailTemplateAdministrationReadService(IEmailTemplateReadModelRepository repository)
    : IEmailTemplateAdministrationReadService {
    public Task<IReadOnlyList<EmailTemplateReadModel>> GetTemplatesAsync(CancellationToken cancellationToken) =>
        repository.GetAllReadModelsAsync(cancellationToken);
}
