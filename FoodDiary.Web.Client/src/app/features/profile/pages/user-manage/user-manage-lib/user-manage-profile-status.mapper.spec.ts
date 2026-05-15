import { describe, expect, it } from 'vitest';

import { buildProfileStatus } from './user-manage-profile-status.mapper';

describe('user manage profile status mapper', () => {
    it('should prioritize dirty form errors', () => {
        expect(
            buildProfileStatus({
                globalError: 'FORM_ERRORS.UNKNOWN',
                isSaving: true,
                isDirty: true,
                isValid: true,
            }),
        ).toEqual({ key: 'USER_MANAGE.PROFILE_STATUS_ERROR', tone: 'danger' });
    });

    it('should build saving status', () => {
        expect(
            buildProfileStatus({
                globalError: null,
                isSaving: true,
                isDirty: false,
                isValid: true,
            }),
        ).toEqual({ key: 'USER_MANAGE.PROFILE_STATUS_SAVING', tone: 'muted' });
    });

    it('should build dirty form statuses', () => {
        expect(
            buildProfileStatus({
                globalError: null,
                isSaving: false,
                isDirty: true,
                isValid: true,
            }),
        ).toEqual({ key: 'USER_MANAGE.PROFILE_STATUS_PENDING', tone: 'warning' });
        expect(
            buildProfileStatus({
                globalError: null,
                isSaving: false,
                isDirty: true,
                isValid: false,
            }),
        ).toEqual({ key: 'USER_MANAGE.PROFILE_STATUS_INVALID', tone: 'warning' });
    });
});
