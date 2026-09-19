using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.UpdateDailyAdviceGroup;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;

namespace FoodDiary.Modules.DailyAdvices.Application.Commands.UpdateDailyAdviceGroup;

public sealed class UpdateDailyAdviceGroupCommandHandler(IDailyAdviceWriteRepository repository)
    : IRequestHandler<UpdateDailyAdviceGroupCommand, Result<DailyAdviceGroupModel>> {
    public async Task<Result<DailyAdviceGroupModel>> Handle(UpdateDailyAdviceGroupCommand request, CancellationToken cancellationToken) {
        DailyAdvice[] replacements;
        try {
            replacements = [DailyAdvice.Create(request.Ru, "ru", request.Weight, request.Tag),
                DailyAdvice.Create(request.En, "en", request.Weight, request.Tag)];
            foreach (DailyAdvice replacement in replacements) {
                replacement.AssignGroup(request.Id);
            }
        } catch (ArgumentException exception) {
            return Result.Failure<DailyAdviceGroupModel>(Errors.Validation.Invalid("advice", exception.Message));
        }
        IReadOnlyList<DailyAdvice> items = await repository.GetGroupAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (items.Count == 0) {
            return Result.Failure<DailyAdviceGroupModel>(DailyAdviceErrors.NotFound());
        }
        var additions = new List<DailyAdvice>();
        foreach (DailyAdvice replacement in replacements) {
            DailyAdvice? item = items.SingleOrDefault(item => string.Equals(item.Locale, replacement.Locale, StringComparison.Ordinal));
            if (item is null) {
                additions.Add(replacement);
            } else {
                item.Update(value: replacement.Value, weight: replacement.Weight,
                    tag: replacement.Tag, clearTag: replacement.Tag is null);
            }
        }
        if (additions.Count > 0) {
            await repository.AddRangeAsync(additions, cancellationToken).ConfigureAwait(false);
        }
        return Result.Success(new DailyAdviceGroupModel(request.Id, replacements[0].Value,
            replacements[1].Value, replacements[0].Weight, replacements[0].Tag));
    }
}
