import type { DietologistRelationship } from '../../models/dietologist.data';
import type { NotificationPreferences, WebPushSubscriptionItem } from '../../models/notification.data';
import type {
    DesiredWaistResponse,
    DesiredWeightResponse,
    GoalHistoryPage,
    User,
    WaistGoalHistoryItem,
    WeightGoalHistoryItem,
} from '../../models/user.data';
import type { UpdateUserDto } from '../../models/user.data';
import { relationshipFromSdk } from './dietologist-sdk.mapper';
import type { ProfileOverviewHttpResponse } from './generated/model/profile-overview-http-response';
import type { UpdateUserHttpRequest } from './generated/model/update-user-http-request';
import type { UserDesiredWaistHttpResponse } from './generated/model/user-desired-waist-http-response';
import type { UserDesiredWeightHttpResponse } from './generated/model/user-desired-weight-http-response';
import type { UserHttpResponse } from './generated/model/user-http-response';
import type { WaistGoalHistoryPageHttpResponse } from './generated/model/waist-goal-history-page-http-response';
import type { WeightGoalHistoryPageHttpResponse } from './generated/model/weight-goal-history-page-http-response';
import { notificationPreferencesFromSdk, webPushSubscriptionFromSdk } from './notification-sdk.mapper';
import { sdkQueryValue } from './sdk-query';
import { requireSdkFields, sdkDefinedFields, sdkEnum, sdkMaybe, sdkNullableFields, sdkOptional } from './sdk-response';

export function userFromSdk(response: UserHttpResponse): User {
    const activityLevel = sdkQueryValue(response.activityLevel);
    const value = requireSdkFields(response, [
        'id',
        'hasPassword',
        'isActive',
        'isEmailConfirmed',
        'pushNotificationsEnabled',
        'fastingPushNotificationsEnabled',
        'socialPushNotificationsEnabled',
        'fastingCheckInReminderHours',
        'fastingCheckInFollowUpReminderHours',
    ]);
    return {
        ...sdkDefinedFields(value, [
            'username',
            'firstName',
            'lastName',
            'birthDate',
            'gender',
            'weightKg',
            'desiredWeightKg',
            'desiredWaistCm',
            'heightCm',
            'dailyCalorieTarget',
            'proteinTarget',
            'fatTarget',
            'carbTarget',
            'fiberTarget',
            'stepGoal',
            'waterGoal',
            'hydrationGoal',
            'language',
            'theme',
            'uiStyle',
            'surfaceStyle',
            'profileImage',
            'profileImageAssetId',
        ]),
        email: value.email ?? null,
        activityLevel:
            activityLevel === undefined
                ? undefined
                : sdkEnum(activityLevel.toUpperCase(), ['MINIMAL', 'LIGHT', 'MODERATE', 'HIGH', 'EXTREME'] as const),
        dashboardLayout: sdkMaybe(value.dashboardLayout, layout => ({ web: layout.web ?? undefined, mobile: layout.mobile ?? undefined })),
    };
}

export function profileOverviewFromSdk(response: ProfileOverviewHttpResponse): {
    user: User;
    notificationPreferences: NotificationPreferences;
    webPushSubscriptions: WebPushSubscriptionItem[];
    dietologistRelationship: DietologistRelationship | null;
} {
    const value = requireSdkFields(response, ['user', 'notificationPreferences', 'webPushSubscriptions']);
    return {
        user: userFromSdk(value.user),
        notificationPreferences: notificationPreferencesFromSdk(value.notificationPreferences),
        webPushSubscriptions: value.webPushSubscriptions.map(webPushSubscriptionFromSdk),
        dietologistRelationship: sdkOptional(value.dietologistRelationship, relationshipFromSdk),
    };
}

export function weightGoalFromSdk(value: UserDesiredWeightHttpResponse): DesiredWeightResponse {
    return sdkNullableFields(value, ['desiredWeightKg', 'startWeightKg', 'startedAtUtc']);
}

export function waistGoalFromSdk(value: UserDesiredWaistHttpResponse): DesiredWaistResponse {
    return sdkNullableFields(value, ['desiredWaistCm', 'startWaistCm', 'startedAtUtc']);
}

export function weightGoalPageFromSdk(response: WeightGoalHistoryPageHttpResponse): GoalHistoryPage<WeightGoalHistoryItem> {
    const value = requireSdkFields(response, ['items']);
    return {
        nextCursor: value.nextCursor ?? null,
        items: value.items.map(row => {
            const item = requireSdkFields(row, ['id', 'targetWeightKg', 'startWeightKg', 'startedAtUtc', 'status']);
            return {
                ...sdkNullableFields(item, ['endWeightKg', 'endedAtUtc']),
                status: sdkEnum(item.status, ['Active', 'Replaced', 'Cancelled'] as const),
            };
        }),
    };
}

export function waistGoalPageFromSdk(response: WaistGoalHistoryPageHttpResponse): GoalHistoryPage<WaistGoalHistoryItem> {
    const value = requireSdkFields(response, ['items']);
    return {
        nextCursor: value.nextCursor ?? null,
        items: value.items.map(row => {
            const item = requireSdkFields(row, ['id', 'targetWaistCm', 'startWaistCm', 'startedAtUtc', 'status']);
            return {
                ...sdkNullableFields(item, ['endWaistCm', 'endedAtUtc']),
                status: sdkEnum(item.status, ['Active', 'Replaced', 'Cancelled'] as const),
            };
        }),
    };
}

export function userUpdateToSdk(data: UpdateUserDto): UpdateUserHttpRequest {
    return {
        timeZoneId: data.timeZoneId,
        username: data.username,
        firstName: data.firstName,
        lastName: data.lastName,
        gender: data.gender,
        heightCm: data.heightCm,
        activityLevel: data.activityLevel,
        stepGoal: data.stepGoal,
        hydrationGoal: data.hydrationGoal,
        language: data.language,
        theme: data.theme,
        uiStyle: data.uiStyle,
        pushNotificationsEnabled: data.pushNotificationsEnabled,
        fastingPushNotificationsEnabled: data.fastingPushNotificationsEnabled,
        socialPushNotificationsEnabled: data.socialPushNotificationsEnabled,
        profileImage: data.profileImage,
        profileImageAssetId: data.profileImageAssetId,
        isActive: data.isActive,
        birthDate: data.birthDate === null ? null : data.birthDate?.toISOString(),
        dashboardLayout: data.dashboardLayout,
    };
}
