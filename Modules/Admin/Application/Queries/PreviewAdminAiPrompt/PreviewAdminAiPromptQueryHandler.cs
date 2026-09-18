using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Ai.Contracts.Queries.PreviewAiPrompt;
using FoodDiary.Results;
namespace FoodDiary.Modules.Admin.Application.Queries.PreviewAdminAiPrompt;

public sealed class PreviewAdminAiPromptQueryHandler(ISender sender) : IRequestHandler<PreviewAdminAiPromptQuery, Result<string>> {
    public Task<Result<string>> Handle(PreviewAdminAiPromptQuery request, CancellationToken cancellationToken) =>
        sender.Send(new PreviewAiPromptQuery(request.Draft.ToOwnerDraft()), cancellationToken);
}
