using FoodDiary.Mediator;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.DeleteDailyAdviceGroup;

namespace FoodDiary.Modules.DailyAdvices.Application.Commands.DeleteDailyAdviceGroup;

public sealed class DeleteDailyAdviceGroupCommandHandler(IDailyAdviceWriteRepository repository)
    : IRequestHandler<DeleteDailyAdviceGroupCommand, Result> {
    public async Task<Result> Handle(DeleteDailyAdviceGroupCommand request, CancellationToken cancellationToken) {
        IReadOnlyList<DailyAdvice> items = await repository.GetGroupAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (items.Count == 0) {
            return Result.Failure(DailyAdviceErrors.NotFound());
        }
        repository.RemoveRange(items);
        return Result.Success();
    }
}
