using FoodDiary.Modules.Identity.Domain.Entities.Content;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;

public interface IEmailTemplateReadRepository {
    Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<EmailTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default);
}
