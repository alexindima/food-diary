import type {
    AttentionSignal,
    BulkRecommendationResult,
    ClientSummary,
    DietologistClientGoals,
    DietologistInvitationForCurrentUser,
    DietologistPermissions,
    DietologistRelationship,
    RecommendationTemplate,
} from '../../models/dietologist.data';
import { optionalCalendarDate, optionalUtcInstant, utcInstant } from '../../models/semantics/date-value';
import { entityId, optionalEntityId } from '../../models/semantics/entity-id';
import type { AttentionSignalHttpResponse } from './generated/model/attention-signal-http-response';
import type { BulkRecommendationResultHttpResponse } from './generated/model/bulk-recommendation-result-http-response';
import type { ClientSummaryHttpResponse } from './generated/model/client-summary-http-response';
import type { DietologistInvitationForCurrentUserHttpResponse } from './generated/model/dietologist-invitation-for-current-user-http-response';
import type { DietologistPermissionsHttpResponse } from './generated/model/dietologist-permissions-http-response';
import type { DietologistRelationshipHttpResponse } from './generated/model/dietologist-relationship-http-response';
import type { RecommendationTemplateHttpResponse } from './generated/model/recommendation-template-http-response';
import type { UserHttpResponse } from './generated/model/user-http-response';
import { requireSdkFields, sdkEnum, sdkNullableFields } from './sdk-response';

function permissionsFromSdk(value: DietologistPermissionsHttpResponse): DietologistPermissions {
    return requireSdkFields(value, [
        'shareProfile',
        'shareMeals',
        'shareStatistics',
        'shareWeight',
        'shareWaist',
        'shareGoals',
        'shareHydration',
        'shareFasting',
    ]);
}

export function relationshipFromSdk(response: DietologistRelationshipHttpResponse): DietologistRelationship {
    const value = requireSdkFields(response, ['invitationId', 'status', 'permissions', 'createdAtUtc', 'expiresAtUtc']);
    return {
        ...sdkNullableFields(value, ['email', 'firstName', 'lastName', 'dietologistUserId', 'acceptedAtUtc']),
        invitationId: entityId<'dietologist-invitation'>(value.invitationId),
        dietologistUserId: optionalEntityId<'user'>(value.dietologistUserId ?? null),
        createdAtUtc: utcInstant(value.createdAtUtc),
        expiresAtUtc: utcInstant(value.expiresAtUtc),
        acceptedAtUtc: optionalUtcInstant(value.acceptedAtUtc ?? null),

        permissions: permissionsFromSdk(value.permissions),
    };
}

export function invitationFromSdk(response: DietologistInvitationForCurrentUserHttpResponse): DietologistInvitationForCurrentUser {
    const mapped = sdkNullableFields(
        requireSdkFields(response, ['invitationId', 'clientUserId', 'status', 'createdAtUtc', 'expiresAtUtc']),
        ['clientEmail', 'clientFirstName', 'clientLastName'],
    );
    return {
        ...mapped,
        invitationId: entityId<'dietologist-invitation'>(mapped.invitationId),
        clientUserId: entityId<'user'>(mapped.clientUserId),
        createdAtUtc: utcInstant(mapped.createdAtUtc),
        expiresAtUtc: utcInstant(mapped.expiresAtUtc),
    };
}

export function clientSummaryFromSdk(response: ClientSummaryHttpResponse): ClientSummary {
    const value = requireSdkFields(response, ['userId', 'permissions', 'acceptedAtUtc']);
    return {
        ...sdkNullableFields(value, ['email', 'firstName', 'lastName', 'profileImage', 'birthDate', 'gender', 'heightCm', 'activityLevel']),
        userId: entityId<'user'>(value.userId),
        birthDate: optionalCalendarDate(value.birthDate ?? null),
        acceptedAtUtc: utcInstant(value.acceptedAtUtc),

        permissions: permissionsFromSdk(value.permissions),
    };
}

export function clientGoalsFromSdk(value: UserHttpResponse): DietologistClientGoals {
    const mapped = sdkNullableFields(requireSdkFields(value, ['id']), ['email']);
    return { ...mapped, id: entityId<'user'>(mapped.id) };
}

export function attentionSignalFromSdk(response: AttentionSignalHttpResponse): AttentionSignal {
    const value = requireSdkFields(response, ['id', 'clientUserId', 'type', 'severity', 'reason', 'detectedAtUtc']);
    return {
        ...sdkNullableFields(value, ['clientDisplayName', 'snoozedUntilUtc']),
        id: entityId<'attention-signal'>(value.id),
        clientUserId: entityId<'user'>(value.clientUserId),
        detectedAtUtc: utcInstant(value.detectedAtUtc),
        snoozedUntilUtc: optionalUtcInstant(value.snoozedUntilUtc ?? null),

        type: sdkEnum(value.type, ['DiaryInactivity', 'CalorieTargetDeviation', 'MaterialWeightChange'] as const),
        severity: sdkEnum(value.severity, ['High', 'Medium', 'Low'] as const),
        reason: sdkEnum(value.reason, [
            'NoRecentDiaryEntries',
            'InsufficientDiaryData',
            'SustainedCalorieTargetDeviation',
            'MaterialWeightChange',
        ] as const),
    };
}

export function recommendationTemplateFromSdk(value: RecommendationTemplateHttpResponse): RecommendationTemplate {
    const mapped = sdkNullableFields(requireSdkFields(value, ['id', 'name', 'text', 'isArchived', 'createdAtUtc']), ['modifiedAtUtc']);
    return {
        ...mapped,
        id: entityId<'recommendation-template'>(mapped.id),
        createdAtUtc: utcInstant(mapped.createdAtUtc),
        modifiedAtUtc: optionalUtcInstant(mapped.modifiedAtUtc),
    };
}

export function bulkRecommendationsFromSdk(response: BulkRecommendationResultHttpResponse): BulkRecommendationResult {
    const value = requireSdkFields(response, ['idempotencyKey', 'recipients']);
    return {
        ...value,
        recipients: value.recipients.map(recipient => {
            const mapped = sdkNullableFields(requireSdkFields(recipient, ['clientUserId', 'succeeded', 'wasAlreadyProcessed']), [
                'recommendationId',
                'errorCode',
            ]);
            return {
                ...mapped,
                clientUserId: entityId<'user'>(mapped.clientUserId),
                recommendationId: optionalEntityId<'recommendation'>(mapped.recommendationId),
            };
        }),
    };
}
