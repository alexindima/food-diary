import type { ProfileStatusViewModel } from './user-manage.types';

export type ProfileStatusState = {
    globalError: string | null;
    isSaving: boolean;
    isDirty: boolean;
    isValid: boolean;
};

export function buildProfileStatus(state: ProfileStatusState): ProfileStatusViewModel {
    if (state.globalError !== null && state.globalError.length > 0 && state.isDirty) {
        return { key: 'USER_MANAGE.PROFILE_STATUS_ERROR', tone: 'danger' };
    }

    if (state.isSaving) {
        return { key: 'USER_MANAGE.PROFILE_STATUS_SAVING', tone: 'muted' };
    }

    if (state.isDirty) {
        return {
            key: state.isValid ? 'USER_MANAGE.PROFILE_STATUS_PENDING' : 'USER_MANAGE.PROFILE_STATUS_INVALID',
            tone: 'warning',
        };
    }

    return { key: 'USER_MANAGE.PROFILE_STATUS_SAVED', tone: 'success' };
}
