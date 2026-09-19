using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvicePairs;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;

namespace FoodDiary.Modules.DailyAdvices.Application.Commands.ImportDailyAdvicePairs;

public sealed class ImportDailyAdvicePairsCommandHandler(IDailyAdviceWriteRepository repository)
    : IRequestHandler<ImportDailyAdvicePairsCommand, Result<DailyAdviceImportModel>> {
    public async Task<Result<DailyAdviceImportModel>> Handle(ImportDailyAdvicePairsCommand request, CancellationToken cancellationToken) {
        if (request.Items is null || request.Items.Count is 0 or > 500) {
            return Invalid("Import must contain between 1 and 500 pairs.");
        }
        var pairs = new List<DailyAdvice[]>();
        var groupIds = new HashSet<Guid>();
        var texts = new HashSet<(string Locale, string Value)>();
        try {
            foreach (DailyAdvicePairImportItem item in request.Items) {
                if (item is null || item.Id == Guid.Empty || !groupIds.Add(item.Id)) {
                    return Invalid("Each pair must have a unique non-empty id.");
                }
                DailyAdvice[] pair = [DailyAdvice.Create(item.Ru, "ru", item.Weight, item.Tag),
                    DailyAdvice.Create(item.En, "en", item.Weight, item.Tag)];
                foreach (DailyAdvice translation in pair) {
                    translation.AssignGroup(item.Id);
                    if (!texts.Add((translation.Locale, translation.Value))) {
                        return Invalid("The file contains repeated translation text.");
                    }
                }
                pairs.Add(pair);
            }
        } catch (ArgumentException exception) {
            return Invalid(exception.Message);
        }

        IReadOnlyList<DailyAdvice> existing = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var groups = existing.GroupBy(item => item.GroupId).ToDictionary(group => group.Key, group => group.ToArray());
        var additions = new List<DailyAdvice>();
        var links = new List<(DailyAdvice Item, Guid GroupId)>();
        int imported = 0;
        // Build the complete plan before mutating any tracked entity. Ambiguous legacy
        // matches and changed reimports must never partially modify the catalog.
        foreach (DailyAdvice[] pair in pairs) {
            Guid groupId = pair[0].GroupId;
            if (groups.TryGetValue(groupId, out DailyAdvice[]? current)) {
                if (current.Length != 2 || pair.Any(expected => !current.Any(item => SameTranslation(item, expected)))) {
                    return Conflict("An existing pair differs from this file. Edit it in the administration page.");
                }
                continue;
            }
            foreach (DailyAdvice translation in pair) {
                DailyAdvice[] matches = [.. existing.Where(item => string.Equals(item.Locale, translation.Locale, StringComparison.Ordinal) && string.Equals(item.Value, translation.Value, StringComparison.Ordinal))];
                if (matches.Length > 1 || matches.Any(item => groups[item.GroupId].Length != 1 || !SameTranslation(item, translation))) {
                    return Conflict("A translation already belongs to another pair, has different metadata, or has ambiguous legacy matches.");
                }
                if (matches.Length == 1) {
                    // Do not take a legacy row from a group explicitly addressed elsewhere in this file.
                    if (groupIds.Contains(matches[0].GroupId)) {
                        return Conflict("A legacy group is also addressed by another pair in this file.");
                    }
                    links.Add((matches[0], groupId));
                } else {
                    additions.Add(translation);
                }
            }
            imported++;
        }
        foreach ((DailyAdvice item, Guid groupId) in links) {
            item.AssignGroup(groupId);
        }
        if (additions.Count > 0) {
            await repository.AddRangeAsync(additions, cancellationToken).ConfigureAwait(false);
        }
        return Result.Success(new DailyAdviceImportModel(imported, pairs.Count - imported));
    }

    private static bool SameTranslation(DailyAdvice left, DailyAdvice right) =>
        string.Equals(left.Locale, right.Locale, StringComparison.Ordinal) && string.Equals(left.Value, right.Value, StringComparison.Ordinal) && string.Equals(left.Tag, right.Tag, StringComparison.Ordinal) && left.Weight == right.Weight;

    private static Result<DailyAdviceImportModel> Invalid(string message) =>
        Result.Failure<DailyAdviceImportModel>(Errors.Validation.Invalid("advices", message));

    private static Result<DailyAdviceImportModel> Conflict(string message) =>
        Result.Failure<DailyAdviceImportModel>(new Error("DailyAdvice.ImportConflict", message, Kind: ErrorKind.Conflict));
}
