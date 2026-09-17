using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvices;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvices;

public sealed class ImportAdminDailyAdvicesCommandHandler(ISender sender)
    : ICommandHandler<ImportAdminDailyAdvicesCommand, Result<AdminDailyAdvicesImportModel>> {
    public async Task<Result<AdminDailyAdvicesImportModel>> Handle(ImportAdminDailyAdvicesCommand command, CancellationToken cancellationToken) {
        DailyAdviceImportItem[] items = [.. command.Advices.Select(item => new DailyAdviceImportItem(item.Value, item.Locale, item.Weight, item.Tag))];
        Result<DailyAdviceImportModel> result = await sender.Send(new ImportDailyAdvicesCommand(items), cancellationToken).ConfigureAwait(false);
        return result.IsFailure
            ? Result.Failure<AdminDailyAdvicesImportModel>(result.Error)
            : Result.Success(new AdminDailyAdvicesImportModel(result.Value.ImportedCount, result.Value.SkippedCount));
    }
}
