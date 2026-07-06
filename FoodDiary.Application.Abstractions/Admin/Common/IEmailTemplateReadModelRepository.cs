using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IEmailTemplateReadModelRepository {
    Task<IReadOnlyList<EmailTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default);
}