using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplates;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplates;

public sealed class GetEmailTemplatesQueryHandler(IEmailTemplateReadModelRepository repository) : IRequestHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateReadModel>> {
    public Task<IReadOnlyList<EmailTemplateReadModel>> Handle(GetEmailTemplatesQuery request, CancellationToken cancellationToken) {
        return repository.GetAllReadModelsAsync(cancellationToken);
    }

}
