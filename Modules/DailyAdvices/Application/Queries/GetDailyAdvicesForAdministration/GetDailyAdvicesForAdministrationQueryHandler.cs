using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvicesForAdministration;

namespace FoodDiary.Modules.DailyAdvices.Application.Queries.GetDailyAdvicesForAdministration;

public sealed class GetDailyAdvicesForAdministrationQueryHandler(IDailyAdviceReadModelRepository repository)
    : IQueryHandler<GetDailyAdvicesForAdministrationQuery, Result<IReadOnlyList<DailyAdviceModel>>> {
    public async Task<Result<IReadOnlyList<DailyAdviceModel>>> Handle(GetDailyAdvicesForAdministrationQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<DailyAdviceReadModel> items = await repository.GetAllReadModelsAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<DailyAdviceModel>>(items.Select(item =>
            new DailyAdviceModel(item.Id, item.Locale, item.Value, item.Tag, item.Weight)).ToArray());
    }
}
