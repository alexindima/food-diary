using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;

namespace FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvices;

public sealed record ImportDailyAdvicesCommand(IReadOnlyList<DailyAdviceImportItem> Items)
    : IRequest<Result<DailyAdviceImportModel>>;
