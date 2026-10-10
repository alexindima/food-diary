import type { HydrationEntryHttpResponse } from '../app/shared/api/sdk/generated/model/hydration-entry-http-response';
import type { UserHttpResponse } from '../app/shared/api/sdk/generated/model/user-http-response';

export const FIXTURE_USER_ID = '00000000-0000-4000-8000-000000000001';
export const FIXTURE_HYDRATION_ID = '00000000-0000-4000-8000-000000000002';

/** Wire values stay native; application adapters decode semantic values. */
export function userFixture(overrides: Partial<UserHttpResponse> = {}): UserHttpResponse {
    return {
        id: FIXTURE_USER_ID,
        hasPassword: true,
        email: 'user@example.test',
        username: 'fixture-user',
        language: 'en',
        theme: 'dark',
        uiStyle: 'classic',
        pushNotificationsEnabled: true,
        fastingPushNotificationsEnabled: true,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: 4,
        fastingCheckInFollowUpReminderHours: 2,
        dashboardLayout: null,
        isActive: true,
        isEmailConfirmed: true,
        aiConsentAcceptedAt: null,
        ...overrides,
    };
}

export function hydrationEntryFixture(overrides: Partial<HydrationEntryHttpResponse> = {}): HydrationEntryHttpResponse {
    return {
        id: FIXTURE_HYDRATION_ID,
        timestampUtc: '2026-04-19T12:00:00Z',
        amountMl: 250,
        ...overrides,
    };
}
