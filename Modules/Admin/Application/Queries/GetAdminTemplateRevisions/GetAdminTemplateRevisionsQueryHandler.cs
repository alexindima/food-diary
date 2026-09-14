using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions;
using FoodDiary.Application.Abstractions.Email.Queries.GetEmailTemplateRevisions;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminTemplateRevisions;

public sealed class GetAdminTemplateRevisionsQueryHandler(ISender sender)
    : IQueryHandler<GetAdminTemplateRevisionsQuery, Result<IReadOnlyList<AdminTemplateRevisionModel>>> {
    public async Task<Result<IReadOnlyList<AdminTemplateRevisionModel>>> Handle(GetAdminTemplateRevisionsQuery query, CancellationToken cancellationToken) {
        if (query.IsAiPrompt) {
            IReadOnlyList<AiPromptRevisionReadModel> revisions = await sender.Send(new GetAiPromptRevisionsQuery(Key: query.Key.Trim().ToLowerInvariant(), Locale: query.Locale.Trim().ToLowerInvariant()), cancellationToken).ConfigureAwait(false);
            return Result.Success<IReadOnlyList<AdminTemplateRevisionModel>>(revisions.Select(item => new AdminTemplateRevisionModel(item.Id, Subject: null, HtmlBody: null, item.PromptText,
                item.IsActive, item.Version, item.SavedOnUtc, item.ArchivedOnUtc)).ToList());
        }
        IReadOnlyList<EmailTemplateRevisionReadModel> emails = await sender.Send(new GetEmailTemplateRevisionsQuery(Key: query.Key.Trim().ToLowerInvariant(), Locale: query.Locale.Trim().ToLowerInvariant()), cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminTemplateRevisionModel>>(emails.Select(item => new AdminTemplateRevisionModel(item.Id, item.Subject, item.HtmlBody, item.TextBody,
            item.IsActive, Version: null, item.SavedOnUtc, item.ArchivedOnUtc)).ToList());
    }
}
