using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects;

public sealed record ReportTarget {
    public ReportTargetType Kind { get; }
    public Guid Id { get; }
    private ReportTarget(ReportTargetType kind, Guid id) {
        Kind = kind;
        Id = id;
    }

    public static ReportTarget ForRecipe(RecipeId id) => new(ReportTargetType.Recipe, id.Value);
    public static ReportTarget ForComment(RecipeCommentId id) => new(ReportTargetType.Comment, id.Value);

    // Read probes retain empty identities and unsupported kinds; aggregate mutations validate both.
    public static ReportTarget FromFields(ReportTargetType targetType, Guid targetId) {
        return targetType switch {
            ReportTargetType.Recipe => ForRecipe(new RecipeId(targetId)),
            ReportTargetType.Comment => ForComment(new RecipeCommentId(targetId)),
            _ => new ReportTarget(targetType, targetId),
        };
    }
}
