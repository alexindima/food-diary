using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvices;

public sealed record ImportAdminDailyAdvicesCommand(int Version, IReadOnlyList<ImportAdminDailyAdviceItem> Advices)
    : ICommand<Result<AdminDailyAdvicesImportModel>>;
