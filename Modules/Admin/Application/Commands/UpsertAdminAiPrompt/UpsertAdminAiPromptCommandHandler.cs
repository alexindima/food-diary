using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Modules.Admin.Application.Commands.UpsertAdminAiPrompt;

public sealed class UpsertAdminAiPromptCommandHandler(IAiPromptAdministrationService administrationService)
    : ICommandHandler<UpsertAdminAiPromptCommand, Result<AdminAiPromptModel>> {
    public async Task<Result<AdminAiPromptModel>> Handle(
        UpsertAdminAiPromptCommand command,
        CancellationToken cancellationToken) {
        string key = command.Key.Trim().ToLowerInvariant();
        Result<string> localeResult = AdminLocaleParser.ParseRequiredLanguage(
            command.Locale,
            nameof(command.Locale),
            "Locale must be one of the supported codes.");
        if (localeResult.IsFailure) {
            return Result.Failure<AdminAiPromptModel>(localeResult.Error);
        }

        Result<AiPromptTemplateReadModel> templateResult = await administrationService.UpsertAsync(
            key,
            localeResult.Value,
            command.PromptText,
            command.IsActive,
            cancellationToken).ConfigureAwait(false);

        return templateResult.IsSuccess
            ? Result.Success(templateResult.Value.ToAdminModel())
            : Result.Failure<AdminAiPromptModel>(templateResult.Error);
    }
}
