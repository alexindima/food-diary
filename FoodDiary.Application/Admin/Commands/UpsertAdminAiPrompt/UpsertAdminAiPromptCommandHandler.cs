using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;

public class UpsertAdminAiPromptCommandHandler(IAiPromptTemplateRepository repository)
    : ICommandHandler<UpsertAdminAiPromptCommand, Result<AdminAiPromptModel>> {
    public async Task<Result<AdminAiPromptModel>> Handle(
        UpsertAdminAiPromptCommand command,
        CancellationToken cancellationToken) {
        var key = command.Key.Trim().ToLowerInvariant();
        var locale = command.Locale.Trim().ToLowerInvariant();

        var existing = await repository.GetByKeyAsync(key, locale, cancellationToken);
        AiPromptTemplate template;

        if (existing is not null) {
            var tracked = await repository.GetByIdAsync(existing.Id, asTracking: true, cancellationToken);
            tracked!.Update(command.PromptText, command.IsActive);
            await repository.UpdateAsync(tracked, cancellationToken);
            template = tracked;
        } else {
            template = AiPromptTemplate.Create(key, locale, command.PromptText, command.IsActive);
            await repository.AddAsync(template, cancellationToken);
        }

        return Result.Success(new AdminAiPromptModel(
            template.Id.Value, template.Key, template.Locale, template.PromptText,
            template.Version, template.IsActive, template.CreatedOnUtc, template.ModifiedOnUtc));
    }
}
