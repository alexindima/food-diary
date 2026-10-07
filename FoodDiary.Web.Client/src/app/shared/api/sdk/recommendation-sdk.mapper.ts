import type { ClientTask, DietologistRecommendation, RecommendationComment } from '../../models/dietologist.data';
import type { ClientTaskHttpResponse } from './generated/model/client-task-http-response';
import type { RecommendationCommentHttpResponse } from './generated/model/recommendation-comment-http-response';
import type { RecommendationHttpResponse } from './generated/model/recommendation-http-response';
import { requireSdkFields, sdkEnum } from './sdk-response';

export function clientTaskFromSdk(response: ClientTaskHttpResponse): ClientTask {
    const value = requireSdkFields(response, ['id', 'dietologistUserId', 'clientUserId', 'title', 'status', 'isOverdue', 'createdAtUtc']);
    return {
        ...value,
        status: sdkEnum(value.status, ['Open', 'Completed', 'Cancelled'] as const),
        details: value.details ?? null,
        dueAtUtc: value.dueAtUtc ?? null,
        statusChangedAtUtc: value.statusChangedAtUtc ?? null,
    };
}

export function recommendationFromSdk(response: RecommendationHttpResponse): DietologistRecommendation {
    const value = requireSdkFields(response, ['id', 'dietologistUserId', 'text', 'isRead', 'createdAtUtc']);
    return {
        ...value,
        dietologistFirstName: value.dietologistFirstName ?? null,
        dietologistLastName: value.dietologistLastName ?? null,
        readAtUtc: value.readAtUtc ?? null,
    };
}

export function recommendationCommentFromSdk(response: RecommendationCommentHttpResponse): RecommendationComment {
    const value = requireSdkFields(response, ['id', 'recommendationId', 'authorUserId', 'text', 'createdAtUtc']);
    return {
        ...value,
        authorFirstName: value.authorFirstName ?? null,
        authorLastName: value.authorLastName ?? null,
        authorEmail: value.authorEmail ?? null,
    };
}
