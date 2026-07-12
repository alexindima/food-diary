using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;

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

        Result<AiPromptTemplate> templateResult = await administrationService.UpsertAsync(
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
