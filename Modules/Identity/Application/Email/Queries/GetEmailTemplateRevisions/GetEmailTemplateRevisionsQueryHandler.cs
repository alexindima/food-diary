using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Email.Queries.GetEmailTemplateRevisions;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Identity.Email.Queries.GetEmailTemplateRevisions;

public sealed class GetEmailTemplateRevisionsQueryHandler(IEmailTemplateReadModelRepository repository) : IRequestHandler<GetEmailTemplateRevisionsQuery, IReadOnlyList<EmailTemplateRevisionReadModel>> {
    public Task<IReadOnlyList<EmailTemplateRevisionReadModel>> Handle(GetEmailTemplateRevisionsQuery request, CancellationToken cancellationToken) {
        string key = request.Key;
        string locale = request.Locale;
        return repository.GetRevisionsAsync(key, locale, cancellationToken);
    }

}
