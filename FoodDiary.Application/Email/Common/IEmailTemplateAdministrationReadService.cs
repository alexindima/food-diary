using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Email.Common;

public interface IEmailTemplateAdministrationReadService {
    Task<IReadOnlyList<EmailTemplateReadModel>> GetTemplatesAsync(CancellationToken cancellationToken);
}
