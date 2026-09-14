using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Admin.Application.Commands.UpsertAdminAiPrompt;

public sealed class UpsertAdminAiPromptCommandHandler(ISender administrationService)
    : ICommandHandler<UpsertAdminAiPromptCommand, Result<AdminAiPromptModel>> {
    public async Task<Result<AdminAiPromptModel>> Handle(
        UpsertAdminAiPromptCommand command,
        CancellationToken cancellationToken) {
        Result<AiPromptTemplateReadModel> templateResult = await administrationService.Send(new UpsertAiPromptCommand(Key: command.Key, Locale: command.Locale, PromptText: command.PromptText, IsActive: command.IsActive), cancellationToken).ConfigureAwait(false);

        return templateResult.IsSuccess
            ? Result.Success(templateResult.Value.ToAdminModel())
            : Result.Failure<AdminAiPromptModel>(templateResult.Error);
    }
}
