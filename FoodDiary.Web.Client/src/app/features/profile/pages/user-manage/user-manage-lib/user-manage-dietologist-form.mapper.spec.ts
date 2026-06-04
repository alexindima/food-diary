import { describe, expect, it } from 'vitest';

import type { DietologistRelationship } from '../../../../../shared/models/dietologist.data';
import { getDietologistPermissions, mapDietologistRelationshipToForm } from './user-manage-dietologist-form.mapper';

const RELATIONSHIP: DietologistRelationship = {
    invitationId: 'invitation-1',
    status: 'Accepted',
    email: 'diet@example.test',
    firstName: null,
    lastName: null,
    dietologistUserId: 'user-1',
    createdAtUtc: '2025-12-01T00:00:00Z',
    acceptedAtUtc: '2026-01-01T00:00:00Z',
    expiresAtUtc: '2026-02-01T00:00:00Z',
    permissions: {
        shareProfile: true,
        shareMeals: false,
        shareStatistics: true,
        shareWeight: false,
        shareWaist: true,
        shareGoals: false,
        shareHydration: true,
        shareFasting: false,
    },
};

describe('user manage dietologist form mapper', () => {
    it('should map accepted relationship into form model', () => {
        const model = mapDietologistRelationshipToForm(RELATIONSHIP);

        expect(model.email).toBe(RELATIONSHIP.email);
        expect(getDietologistPermissions(model)).toEqual(RELATIONSHIP.permissions);
    });

    it('should map empty relationship into invite defaults', () => {
        const model = mapDietologistRelationshipToForm(null);

        expect(model.email).toBe('');
        expect(getDietologistPermissions(model)).toEqual({
            shareProfile: true,
            shareMeals: true,
            shareStatistics: true,
            shareWeight: true,
            shareWaist: true,
            shareGoals: true,
            shareHydration: true,
            shareFasting: true,
        });
    });
});
