import type { ClientTask, DietologistRecommendation, RecommendationComment } from '../../models/dietologist.data';
import { optionalUtcInstant, utcInstant } from '../../models/semantics/date-value';
import { entityId } from '../../models/semantics/entity-id';
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
        dueAtUtc: optionalUtcInstant(value.dueAtUtc ?? null),
        statusChangedAtUtc: optionalUtcInstant(value.statusChangedAtUtc ?? null),

        id: entityId<'client-task'>(value.id),
        dietologistUserId: entityId<'user'>(value.dietologistUserId),
        clientUserId: entityId<'user'>(value.clientUserId),
        createdAtUtc: utcInstant(value.createdAtUtc),
    };
}

export function recommendationFromSdk(response: RecommendationHttpResponse): DietologistRecommendation {
    const value = requireSdkFields(response, ['id', 'dietologistUserId', 'text', 'isRead', 'createdAtUtc']);
    return {
        ...value,
        dietologistFirstName: value.dietologistFirstName ?? null,
        dietologistLastName: value.dietologistLastName ?? null,
        readAtUtc: optionalUtcInstant(value.readAtUtc ?? null),

        id: entityId<'recommendation'>(value.id),
        dietologistUserId: entityId<'user'>(value.dietologistUserId),
        createdAtUtc: utcInstant(value.createdAtUtc),
    };
}

export function recommendationCommentFromSdk(response: RecommendationCommentHttpResponse): RecommendationComment {
    const value = requireSdkFields(response, ['id', 'recommendationId', 'authorUserId', 'text', 'createdAtUtc']);
    return {
        ...value,
        authorFirstName: value.authorFirstName ?? null,
        authorLastName: value.authorLastName ?? null,
        authorEmail: value.authorEmail ?? null,

        id: entityId<'recommendation-comment'>(value.id),
        recommendationId: entityId<'recommendation'>(value.recommendationId),
        authorUserId: entityId<'user'>(value.authorUserId),
        createdAtUtc: utcInstant(value.createdAtUtc),
    };
}
