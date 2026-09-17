using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvices;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;

namespace FoodDiary.Modules.DailyAdvices.Application.Commands.ImportDailyAdvices;

public sealed class ImportDailyAdvicesCommandHandler(
    IDailyAdviceReadModelRepository readRepository,
    IDailyAdviceWriteRepository writeRepository)
    : IRequestHandler<ImportDailyAdvicesCommand, Result<DailyAdviceImportModel>> {
    public async Task<Result<DailyAdviceImportModel>> Handle(ImportDailyAdvicesCommand request, CancellationToken cancellationToken) {
        if (request.Items is null || request.Items.Count is 0 or > 1000) {
            return Result.Failure<DailyAdviceImportModel>(Errors.Validation.Invalid("advices", "Import must contain between 1 and 1000 advices."));
        }

        var parsed = new List<DailyAdvice>(request.Items.Count);
        for (int index = 0; index < request.Items.Count; index++) {
            DailyAdviceImportItem? item = request.Items[index];
            try {
                if (item is null) {
                    throw new ArgumentException("Advice must not be null.", nameof(request));
                }
                parsed.Add(DailyAdvice.Create(item.Value, item.Locale, item.Weight, item.Tag));
            } catch (ArgumentException exception) {
                return Result.Failure<DailyAdviceImportModel>(Errors.Validation.Invalid(
                    string.Create(CultureInfo.InvariantCulture, $"advices[{index}]"), exception.Message));
            }
        }

        IReadOnlyList<DailyAdviceReadModel> existing = await readRepository.GetAllReadModelsAsync(cancellationToken).ConfigureAwait(false);
        HashSet<(string Value, string Locale, int Weight, string? Tag)> identities = [.. existing.Select(item => (item.Value, item.Locale, item.Weight, item.Tag))];
        var additions = new List<DailyAdvice>();
        foreach (DailyAdvice advice in parsed) {
            if (identities.Add((advice.Value, advice.Locale, advice.Weight, advice.Tag))) {
                additions.Add(advice);
            }
        }
        if (additions.Count > 0) {
            await writeRepository.AddRangeAsync(additions, cancellationToken).ConfigureAwait(false);
        }
        return Result.Success(new DailyAdviceImportModel(additions.Count, parsed.Count - additions.Count));
    }
}
