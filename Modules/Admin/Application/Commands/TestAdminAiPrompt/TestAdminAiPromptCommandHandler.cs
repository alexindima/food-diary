using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Ai.Contracts.Commands.TestAiPrompt;
using FoodDiary.Results;
namespace FoodDiary.Modules.Admin.Application.Commands.TestAdminAiPrompt;

public sealed class TestAdminAiPromptCommandHandler(ISender sender) : IRequestHandler<TestAdminAiPromptCommand, Result<string>> {
    public Task<Result<string>> Handle(TestAdminAiPromptCommand request, CancellationToken cancellationToken) =>
        sender.Send(new TestAiPromptCommand(request.UserId, request.RequestId, request.Draft.ToOwnerDraft()), cancellationToken);
}
