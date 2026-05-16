import { describe, expect, it } from 'vitest';

import { createClient } from '../../clients/dietologist-clients-lib/dietologist-clients.mapper.spec';
import { buildClientDashboardSections, buildClientProfileChips, getClientDashboardTitle } from './client-dashboard.mapper';

describe('client dashboard mapper', () => {
    it('resolves title from full name or email fallback', () => {
        expect(getClientDashboardTitle(createClient({ firstName: 'Alex', lastName: 'Ivanov' }))).toBe('Alex Ivanov');
        expect(getClientDashboardTitle(createClient({ firstName: null, lastName: null, email: 'client@example.com' }))).toBe(
            'client@example.com',
        );
    });

    it('returns profile chips only when profile sharing is enabled', () => {
        expect(buildClientProfileChips(createClient())).toEqual(['180 cm', 'Male', 'Moderate']);
        expect(buildClientProfileChips(createClient({ permissions: { ...createClient().permissions, shareProfile: false } }))).toEqual([]);
    });

    it('returns sections matching shared permissions', () => {
        const sections = buildClientDashboardSections(createClient());

        expect(sections.map(section => section.titleKey)).toEqual([
            'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.PROFILE_TITLE',
            'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.MEALS_TITLE',
        ]);
    });
});
