using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;

namespace FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvicePairs;

public sealed record ImportDailyAdvicePairsCommand(IReadOnlyList<DailyAdvicePairImportItem> Items) : IRequest<Result<DailyAdviceImportModel>>;
