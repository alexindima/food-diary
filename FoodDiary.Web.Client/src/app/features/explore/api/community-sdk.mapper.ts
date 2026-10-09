import type { ContentReportHttpResponse } from '../../../shared/api/sdk/generated/model/content-report-http-response';
import type { CreateContentReportHttpRequest } from '../../../shared/api/sdk/generated/model/create-content-report-http-request';
import type { RecipeCommentHttpResponse } from '../../../shared/api/sdk/generated/model/recipe-comment-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { optionalUtcInstant, utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { RecipeComment } from '../models/comment.data';
import type { ContentReport, CreateReportDto, ObservedReportTarget } from '../models/report.data';

export function commentFromSdk(response: RecipeCommentHttpResponse): RecipeComment {
    const value = requireSdkFields(response, ['id', 'recipeId', 'authorId', 'text', 'createdAtUtc', 'isOwnedByCurrentUser']);
    return {
        ...value,
        id: entityId<'recipe-comment'>(value.id),
        recipeId: entityId<'recipe'>(value.recipeId),
        authorId: entityId<'user'>(value.authorId),
        createdAtUtc: utcInstant(value.createdAtUtc),
        modifiedAtUtc: optionalUtcInstant(value.modifiedAtUtc),
    };
}

export function createReportToSdk(dto: CreateReportDto): CreateContentReportHttpRequest {
    const target = dto.target;
    return {
        targetType: target.kind === 'recipe' ? 'Recipe' : 'Comment',
        targetId: target.kind === 'recipe' ? target.recipeId : target.commentId,
        reason: dto.reason,
    };
}

export function contentReportFromSdk(response: ContentReportHttpResponse): ContentReport {
    const { targetType, targetId, ...value } = requireSdkFields(response, [
        'id',
        'reporterId',
        'targetType',
        'targetId',
        'reason',
        'status',
        'createdAtUtc',
    ]);
    return {
        ...value,
        id: entityId<'content-report'>(value.id),
        reporterId: entityId<'user'>(value.reporterId),
        target: reportTargetFromSdk(targetType, targetId),
        createdAtUtc: utcInstant(value.createdAtUtc),
        reviewedAtUtc: optionalUtcInstant(value.reviewedAtUtc),
    };
}

function reportTargetFromSdk(targetType: string, targetId: string): ObservedReportTarget {
    switch (targetType) {
        case 'Recipe': {
            return { kind: 'recipe', recipeId: entityId<'recipe'>(targetId) };
        }
        case 'Comment': {
            return { kind: 'comment', commentId: entityId<'recipe-comment'>(targetId) };
        }
        default: {
            return { kind: 'unknown', targetType, targetId };
        }
    }
}
