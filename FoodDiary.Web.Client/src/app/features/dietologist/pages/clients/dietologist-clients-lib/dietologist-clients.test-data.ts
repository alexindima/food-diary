import type { ClientSummary } from '../../../../../shared/models/dietologist.data';

export const VALID_CLIENT_ACCEPTED_AT_UTC = '2026-05-16T10:00:00.000Z';

export function createClient(overrides: Partial<ClientSummary> = {}): ClientSummary {
    return {
        userId: 'client-1',
        email: 'client@example.com',
        firstName: 'Alex',
        lastName: 'Ivanov',
        profileImage: null,
        birthDate: null,
        gender: 'Male',
        height: 180,
        activityLevel: 'Moderate',
        acceptedAtUtc: VALID_CLIENT_ACCEPTED_AT_UTC,
        permissions: {
            shareProfile: true,
            shareMeals: true,
            shareStatistics: false,
            shareWeight: false,
            shareWaist: false,
            shareGoals: false,
            shareHydration: false,
            shareFasting: false,
        },
        ...overrides,
    };
}
