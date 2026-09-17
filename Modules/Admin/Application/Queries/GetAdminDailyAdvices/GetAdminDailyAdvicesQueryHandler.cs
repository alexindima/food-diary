using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvicesForAdministration;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminDailyAdvices;

public sealed class GetAdminDailyAdvicesQueryHandler(ISender sender)
    : IQueryHandler<GetAdminDailyAdvicesQuery, Result<IReadOnlyList<AdminDailyAdviceModel>>> {
    public async Task<Result<IReadOnlyList<AdminDailyAdviceModel>>> Handle(GetAdminDailyAdvicesQuery query, CancellationToken cancellationToken) {
        Result<IReadOnlyList<DailyAdviceModel>> result = await sender.Send(new GetDailyAdvicesForAdministrationQuery(), cancellationToken).ConfigureAwait(false);
        return result.IsFailure
            ? Result.Failure<IReadOnlyList<AdminDailyAdviceModel>>(result.Error)
            : Result.Success<IReadOnlyList<AdminDailyAdviceModel>>(result.Value.Select(item =>
                new AdminDailyAdviceModel(item.Id, item.Locale, item.Value, item.Tag, item.Weight)).ToArray());
    }
}
