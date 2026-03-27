using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;

public sealed class GetAdminEmailTemplatesQueryHandler(
    IEmailTemplateRepository repository)
    : IQueryHandler<GetAdminEmailTemplatesQuery, Result<IReadOnlyList<AdminEmailTemplateModel>>> {
    public async Task<Result<IReadOnlyList<AdminEmailTemplateModel>>> Handle(
        GetAdminEmailTemplatesQuery query,
        CancellationToken cancellationToken) {
        var templates = await repository.GetAllAsync(cancellationToken);
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
