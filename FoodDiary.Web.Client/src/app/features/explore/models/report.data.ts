import type { UtcInstant } from '../../../shared/models/semantics/date-value';
import type { ContentReportId, RecipeCommentId, RecipeId, UserId } from '../../../shared/models/semantics/entity-id';

export type ReportTarget = { kind: 'recipe'; recipeId: RecipeId } | { kind: 'comment'; commentId: RecipeCommentId };

/** Preserve future server target types as observations, outside the mutation contract. */
export type ObservedReportTarget = ReportTarget | { kind: 'unknown'; targetType: string; targetId: string };

export type ContentReport = {
    id: ContentReportId;
    reporterId: UserId;
    target: ObservedReportTarget;
    reason: string;
    status: string;
    adminNote?: string | null;
    createdAtUtc: UtcInstant;
    reviewedAtUtc?: UtcInstant | null;
};

export type CreateReportDto = {
    target: ReportTarget;
    reason: string;
};
