import type { ClientSummary } from '../../../../../shared/models/dietologist.data';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';

export const VALID_CLIENT_ACCEPTED_AT_UTC = '2026-05-16T10:00:00.000Z';

export function createClient(overrides: Partial<ClientSummary> = {}): ClientSummary {
    return {
        userId: entityId<'user'>('client-1'),
        email: 'client@example.com',
        firstName: 'Alex',
        lastName: 'Ivanov',
        profileImage: null,
        birthDate: null,
        gender: 'Male',
        heightCm: 180,
        activityLevel: 'Moderate',
        acceptedAtUtc: utcInstant(VALID_CLIENT_ACCEPTED_AT_UTC),
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
