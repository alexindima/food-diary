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
        permissions: permissionsFromSdk(value.permissions),
    };
}

export function invitationFromSdk(response: DietologistInvitationForCurrentUserHttpResponse): DietologistInvitationForCurrentUser {
    return sdkNullableFields(requireSdkFields(response, ['invitationId', 'clientUserId', 'status', 'createdAtUtc', 'expiresAtUtc']), [
        'clientEmail',
        'clientFirstName',
        'clientLastName',
    ]);
}

export function clientSummaryFromSdk(response: ClientSummaryHttpResponse): ClientSummary {
    const value = requireSdkFields(response, ['userId', 'permissions', 'acceptedAtUtc']);
    return {
        ...sdkNullableFields(value, ['email', 'firstName', 'lastName', 'profileImage', 'birthDate', 'gender', 'heightCm', 'activityLevel']),
        permissions: permissionsFromSdk(value.permissions),
    };
}

export function clientGoalsFromSdk(value: UserHttpResponse): DietologistClientGoals {
    return sdkNullableFields(requireSdkFields(value, ['id']), ['email']);
}

export function attentionSignalFromSdk(response: AttentionSignalHttpResponse): AttentionSignal {
    const value = requireSdkFields(response, ['id', 'clientUserId', 'type', 'severity', 'reason', 'detectedAtUtc']);
    return {
        ...sdkNullableFields(value, ['clientDisplayName', 'snoozedUntilUtc']),
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
    return sdkNullableFields(requireSdkFields(value, ['id', 'name', 'text', 'isArchived', 'createdAtUtc']), ['modifiedAtUtc']);
}

export function bulkRecommendationsFromSdk(response: BulkRecommendationResultHttpResponse): BulkRecommendationResult {
    const value = requireSdkFields(response, ['idempotencyKey', 'recipients']);
    return {
        ...value,
        recipients: value.recipients.map(recipient =>
            sdkNullableFields(requireSdkFields(recipient, ['clientUserId', 'succeeded', 'wasAlreadyProcessed']), [
                'recommendationId',
                'errorCode',
            ]),
        ),
    };
}
