import { describe, expect, it } from 'vitest';

import type { DietologistRelationship } from '../../../../shared/models/dietologist.data';
import { getDietologistPermissions, syncDietologistFormFromRelationship } from './user-manage-dietologist-form.mapper';
import { createDietologistForm } from './user-manage-form.mapper';

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
    it('should sync accepted relationship into disabled form', () => {
        const form = createDietologistForm();

        syncDietologistFormFromRelationship(form, RELATIONSHIP);

        expect(form.controls.email.disabled).toBe(true);
        expect(form.controls.email.getRawValue()).toBe(RELATIONSHIP.email);
        expect(getDietologistPermissions(form)).toEqual(RELATIONSHIP.permissions);
        expect(form.pristine).toBe(true);
        expect(form.untouched).toBe(true);
    });

    it('should reset empty relationship into invite defaults', () => {
        const form = createDietologistForm();
        syncDietologistFormFromRelationship(form, RELATIONSHIP);

        syncDietologistFormFromRelationship(form, null);

        expect(form.controls.email.enabled).toBe(true);
        expect(form.controls.email.value).toBe('');
        expect(getDietologistPermissions(form)).toEqual({
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
