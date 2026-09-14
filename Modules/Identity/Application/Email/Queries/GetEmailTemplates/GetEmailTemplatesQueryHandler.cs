using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Email.Queries.GetEmailTemplates;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Identity.Email.Queries.GetEmailTemplates;

public sealed class GetEmailTemplatesQueryHandler(IEmailTemplateReadModelRepository repository) : IRequestHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateReadModel>> {
    public Task<IReadOnlyList<EmailTemplateReadModel>> Handle(GetEmailTemplatesQuery request, CancellationToken cancellationToken) {
        return repository.GetAllReadModelsAsync(cancellationToken);
    }

}
