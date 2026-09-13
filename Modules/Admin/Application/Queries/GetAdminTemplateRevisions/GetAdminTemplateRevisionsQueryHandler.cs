using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminTemplateRevisions;

public sealed class GetAdminTemplateRevisionsQueryHandler(IEmailTemplateAdministrationReadService emailTemplateReadService, IAiAdministrationReadService aiReadService)
    : IQueryHandler<GetAdminTemplateRevisionsQuery, Result<IReadOnlyList<AdminTemplateRevisionModel>>> {
    public async Task<Result<IReadOnlyList<AdminTemplateRevisionModel>>> Handle(GetAdminTemplateRevisionsQuery query, CancellationToken cancellationToken) {
        if (query.IsAiPrompt) {
            IReadOnlyList<AiPromptRevisionReadModel> revisions = await aiReadService.GetPromptRevisionsAsync(query.Key.Trim().ToLowerInvariant(), query.Locale.Trim().ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
            return Result.Success<IReadOnlyList<AdminTemplateRevisionModel>>(revisions.Select(item => new AdminTemplateRevisionModel(item.Id, Subject: null, HtmlBody: null, item.PromptText,
                item.IsActive, item.Version, item.SavedOnUtc, item.ArchivedOnUtc)).ToList());
        }
        IReadOnlyList<EmailTemplateRevisionReadModel> emails = await emailTemplateReadService.GetRevisionsAsync(query.Key.Trim().ToLowerInvariant(), query.Locale.Trim().ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminTemplateRevisionModel>>(emails.Select(item => new AdminTemplateRevisionModel(item.Id, item.Subject, item.HtmlBody, item.TextBody,
            item.IsActive, Version: null, item.SavedOnUtc, item.ArchivedOnUtc)).ToList());
    }
}
