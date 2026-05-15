import { describe, expect, it } from 'vitest';

import { Gender, type User } from '../../../../../shared/models/user.data';
import { buildUserManageSelectOptions, createDietologistForm, createUserManageForm, mapUserToForm } from './user-manage-form.mapper';

const USER: User = {
    id: 'user-1',
    email: 'user@example.test',
    hasPassword: true,
    username: 'alex',
    firstName: 'Alex',
    lastName: 'Ivanov',
    birthDate: new Date('1990-05-10T12:00:00Z'),
    gender: 'M',
    height: 180,
    activityLevel: 'MODERATE',
    stepGoal: 8000,
    language: 'ru-RU',
    theme: 'default',
    uiStyle: 'default',
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
    profileImage: 'https://example.test/avatar.png',
    profileImageAssetId: 'asset-1',
    isActive: true,
    isEmailConfirmed: true,
};

describe('user manage form mapper', () => {
    it('should create user form with empty defaults', () => {
        const form = createUserManageForm();

        expect(form.controls.email.disabled).toBe(true);
        expect(form.controls.email.value).toBe('');
        expect(form.controls.username.value).toBeNull();
        expect(form.controls.profileImage.value).toBeNull();
    });

    it('should create dietologist form with default shared permissions', () => {
        const form = createDietologistForm();

        expect(form.controls.email.value).toBe('');
        expect(form.controls.shareProfile.value).toBe(true);
        expect(form.controls.shareMeals.value).toBe(true);
        expect(form.controls.shareFasting.value).toBe(true);
    });

    it('should normalize legacy user values into form patch', () => {
        expect(mapUserToForm(USER)).toEqual({
            email: USER.email,
            username: USER.username,
            firstName: USER.firstName,
            lastName: USER.lastName,
            gender: Gender.Male,
            language: 'ru',
            theme: 'ocean',
            uiStyle: 'classic',
            birthDate: '1990-05-10',
            height: USER.height,
            activityLevel: USER.activityLevel,
            stepGoal: USER.stepGoal,
            profileImage: {
                url: USER.profileImage,
                assetId: USER.profileImageAssetId,
            },
            pushNotificationsEnabled: USER.pushNotificationsEnabled,
            fastingPushNotificationsEnabled: USER.fastingPushNotificationsEnabled,
            socialPushNotificationsEnabled: USER.socialPushNotificationsEnabled,
        });
    });

    it('should drop unsupported optional values', () => {
        expect(
            mapUserToForm({
                ...USER,
                gender: 'unknown',
                language: '',
                theme: 'unsupported',
                uiStyle: 'unsupported',
                activityLevel: undefined,
                profileImage: '',
                profileImageAssetId: undefined,
            }),
        ).toMatchObject({
            gender: null,
            language: null,
            theme: null,
            uiStyle: null,
            activityLevel: null,
            profileImage: null,
        });
    });

    it('should build translated select options', () => {
        const options = buildUserManageSelectOptions(key => `translated:${key}`);

        expect(options.genderOptions).toContainEqual({
            label: 'translated:USER_MANAGE.GENDER_OPTIONS.M',
            value: Gender.Male,
        });
        expect(options.activityLevelOptions[0]).toEqual({
            label: 'translated:USER_MANAGE.ACTIVITY_LEVEL_OPTIONS.MINIMAL',
            value: 'MINIMAL',
        });
        expect(options.languageOptions).toEqual([
            { label: 'translated:USER_MANAGE.LANGUAGE_OPTIONS.EN', value: 'en' },
            { label: 'translated:USER_MANAGE.LANGUAGE_OPTIONS.RU', value: 'ru' },
        ]);
    });
});
