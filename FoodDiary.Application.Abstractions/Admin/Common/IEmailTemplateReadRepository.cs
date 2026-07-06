using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IEmailTemplateReadRepository {
    Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default);

    Task<EmailTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default);
}
