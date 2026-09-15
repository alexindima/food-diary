using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplateRevisions;

public sealed class GetEmailTemplateRevisionsQueryHandler(IEmailTemplateReadModelRepository repository) : IRequestHandler<GetEmailTemplateRevisionsQuery, IReadOnlyList<EmailTemplateRevisionReadModel>> {
    public Task<IReadOnlyList<EmailTemplateRevisionReadModel>> Handle(GetEmailTemplateRevisionsQuery request, CancellationToken cancellationToken) {
        string key = request.Key;
        string locale = request.Locale;
        return repository.GetRevisionsAsync(key, locale, cancellationToken);
    }

}
