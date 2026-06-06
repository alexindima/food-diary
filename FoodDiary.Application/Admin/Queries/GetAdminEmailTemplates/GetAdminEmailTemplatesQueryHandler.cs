using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;

public sealed class GetAdminEmailTemplatesQueryHandler(
    IEmailTemplateRepository repository)
    : IQueryHandler<GetAdminEmailTemplatesQuery, Result<IReadOnlyList<AdminEmailTemplateModel>>> {
    public async Task<Result<IReadOnlyList<AdminEmailTemplateModel>>> Handle(
        GetAdminEmailTemplatesQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<EmailTemplate> templates = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var response = templates
            .Select(t => new AdminEmailTemplateModel(
                t.Id,
                t.Key,
                t.Locale,
                t.Subject,
                t.HtmlBody,
                t.TextBody,
                t.IsActive,
                t.CreatedOnUtc,
                t.ModifiedOnUtc))
            .ToList();

        return Result.Success<IReadOnlyList<AdminEmailTemplateModel>>(response);
    }
}
