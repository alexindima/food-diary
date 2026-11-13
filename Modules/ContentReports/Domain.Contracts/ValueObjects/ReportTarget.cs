using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects;

public closed record class ReportTarget {
    private ReportTarget() { }

    public ReportTargetType Kind => this switch {
        Recipe => ReportTargetType.Recipe,
        Comment => ReportTargetType.Comment,
        Unsupported unsupported => unsupported.TargetType,
    };

    public Guid Id => this switch {
        Recipe recipe => recipe.RecipeId.Value,
        Comment comment => comment.CommentId.Value,
        Unsupported unsupported => unsupported.TargetId,
    };

    public static ReportTarget ForRecipe(RecipeId id) => new Recipe(id);
    public static ReportTarget ForComment(RecipeCommentId id) => new Comment(id);

    // Read probes retain empty identities and unsupported kinds; aggregate mutations validate both.
    public static ReportTarget FromFields(ReportTargetType targetType, Guid targetId) {
        return targetType switch {
            ReportTargetType.Recipe => ForRecipe(new RecipeId(targetId)),
            ReportTargetType.Comment => ForComment(new RecipeCommentId(targetId)),
            _ => new Unsupported(targetType, targetId),
        };
    }

    private sealed record Recipe(RecipeId RecipeId) : ReportTarget;
    private sealed record Comment(RecipeCommentId CommentId) : ReportTarget;
    private sealed record Unsupported(ReportTargetType TargetType, Guid TargetId) : ReportTarget;
}
