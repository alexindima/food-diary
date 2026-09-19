using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvicePairs;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvicePairs;

public sealed class ImportAdminDailyAdvicePairsCommandHandler(ISender sender) : ICommandHandler<ImportAdminDailyAdvicePairsCommand, Result<AdminDailyAdvicesImportModel>> {
    public async Task<Result<AdminDailyAdvicesImportModel>> Handle(ImportAdminDailyAdvicePairsCommand command, CancellationToken cancellationToken) {
        if (command.Version != 2) {
            return Result.Failure<AdminDailyAdvicesImportModel>(FoodDiary.Application.Abstractions.Common.Abstractions.Results.Errors.Validation.Invalid("version", "Expected format version 2."));
        }
        Result<DailyAdviceImportModel> result = await sender.Send(new ImportDailyAdvicePairsCommand(
            command.Advices?.Select(item => item is null ? null! : new DailyAdvicePairImportItem(item.Id, item.Ru, item.En, item.Weight, item.Tag)).ToArray()!), cancellationToken).ConfigureAwait(false);
        return result.IsFailure ? Result.Failure<AdminDailyAdvicesImportModel>(result.Error)
            : Result.Success(new AdminDailyAdvicesImportModel(result.Value.ImportedCount, result.Value.SkippedCount));
    }
}
