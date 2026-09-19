using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvicePairs;

public sealed record ImportAdminDailyAdvicePairsCommand(int Version, IReadOnlyList<ImportAdminDailyAdvicePairItem> Advices) : ICommand<Result<AdminDailyAdvicesImportModel>>;
