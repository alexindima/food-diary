using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Admin;

namespace FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;

public sealed class GetAdminEmailTemplatesQueryHandler(
    IEmailTemplateRepository repository)
    : IQueryHandler<GetAdminEmailTemplatesQuery, Result<IReadOnlyList<AdminEmailTemplateResponse>>>
{
    public async Task<Result<IReadOnlyList<AdminEmailTemplateResponse>>> Handle(
        GetAdminEmailTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        var templates = await repository.GetAllAsync(cancellationToken);
        var response = templates
            .Select(t => new AdminEmailTemplateResponse(
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

        return Result.Success<IReadOnlyList<AdminEmailTemplateResponse>>(response);
    }
}
