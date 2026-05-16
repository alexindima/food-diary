import { describe, expect, it } from 'vitest';

import type { ClientSummary } from '../../../../../shared/models/dietologist.data';
import { buildClientCardViewModels, formatClientConnectedDate, getClientInitials, getClientTitle } from './dietologist-clients.mapper';

const VALID_DATE = '2026-05-16T10:00:00.000Z';

describe('dietologist clients mapper', () => {
    it('builds client titles from full name or email fallback', () => {
        expect(getClientTitle(createClient({ firstName: 'Alex', lastName: 'Ivanov' }))).toBe('Alex Ivanov');
        expect(getClientTitle(createClient({ firstName: null, lastName: null, email: 'client@example.com' }))).toBe('client@example.com');
    });

    it('builds initials from profile names or email fallback', () => {
        expect(getClientInitials(createClient({ firstName: ' Alex ', lastName: ' Ivanov ' }))).toBe('AI');
        expect(getClientInitials(createClient({ firstName: null, lastName: null, email: 'client@example.com' }))).toBe('C');
    });

    it('keeps invalid dates unchanged', () => {
        expect(formatClientConnectedDate('not-a-date', 'en')).toBe('not-a-date');
    });

    it('builds client card view models', () => {
        const result = buildClientCardViewModels([createClient()], 'en');

        expect(result).toHaveLength(1);
        expect(result[0].title).toBe('Alex Ivanov');
        expect(result[0].initials).toBe('AI');
        expect(result[0].connectedDateLabel).not.toBe(VALID_DATE);
    });
});

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
        acceptedAtUtc: VALID_DATE,
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
