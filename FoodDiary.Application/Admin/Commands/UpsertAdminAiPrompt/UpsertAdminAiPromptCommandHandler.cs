using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;

public class UpsertAdminAiPromptCommandHandler(IAiPromptTemplateWriteRepository repository)
    : ICommandHandler<UpsertAdminAiPromptCommand, Result<AdminAiPromptModel>> {
    public async Task<Result<AdminAiPromptModel>> Handle(
        UpsertAdminAiPromptCommand command,
        CancellationToken cancellationToken) {
        string key = command.Key.Trim().ToLowerInvariant();
        Result<string> localeResult = StringCodeParser.ParseRequiredLanguage(
            command.Locale,
            nameof(command.Locale),
            "Locale must be one of the supported codes.");
        if (localeResult.IsFailure) {
            return Result.Failure<AdminAiPromptModel>(localeResult.Error);
        }

        AiPromptTemplate? existing = await repository.GetByKeyAsync(key, localeResult.Value, cancellationToken).ConfigureAwait(false);
        AiPromptTemplate template;

        if (existing is not null) {
            AiPromptTemplate? tracked = await repository.GetByIdAsync(existing.Id, asTracking: true, cancellationToken).ConfigureAwait(false);
            tracked!.Update(command.PromptText, command.IsActive);
            await repository.UpdateAsync(tracked, cancellationToken).ConfigureAwait(false);
            template = tracked;
        } else {
            template = AiPromptTemplate.Create(key, localeResult.Value, command.PromptText, command.IsActive);
            await repository.AddAsync(template, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(template.ToAdminModel());
    }
}
