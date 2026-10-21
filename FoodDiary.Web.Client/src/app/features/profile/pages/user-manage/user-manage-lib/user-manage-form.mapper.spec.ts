import { describe, expect, it } from 'vitest';

import { imageSelection } from '../../../../../shared/models/image-upload.data';
import { calendarDate } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { Gender, UpdateUserDto, type User } from '../../../../../shared/models/user.data';
import {
    buildUserManageSelectOptions,
    buildUserUpdateDto,
    createDietologistFormModel,
    createUserManageFormModel,
    mapUserToForm,
    normalizeOptionalTextInput,
    parseOptionalNumberInput,
} from '../../../lib/user-manage-form.mapper';

const USER: User = {
    id: entityId<'user'>('user-1'),
    email: 'user@example.test',
    hasPassword: true,
    username: 'alex',
    firstName: 'Alex',
    lastName: 'Ivanov',
    birthDate: calendarDate('1990-05-10T12:00:00Z'),
    gender: 'M',
    heightCm: 180,
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
    profileImageAssetId: entityId<'image-asset'>('asset-1'),
    isActive: true,
    isEmailConfirmed: true,
};

const EXPECTED_HEIGHT = 180;

describe('profile birth date calendar contract', () => {
    it.each([
        ['1990-01-02', '1990-01-02'],
        ['1990-01-02T00:00:00Z', '1990-01-02'],
        ['1990-01-02T23:30:00Z', '1990-01-02'],
        ['2000-02-29T00:00:00Z', '2000-02-29'],
        ['2000-12-31T23:30:00Z', '2000-12-31'],
    ])('preserves the day label from %s through an unrelated profile save', (encoded, expected) => {
        const form = { ...createUserManageFormModel(), ...mapUserToForm({ ...USER, birthDate: calendarDate(encoded) }) };

        expect(form.birthDate).toBe(expected);
        expect(buildUserUpdateDto({ ...form, firstName: 'Changed name' }).birthDate?.toISOString()).toBe(`${expected}T00:00:00.000Z`);
    });

    it.each(['not-a-date', '2025-02-29'])('does not put an invalid calendar date %s into the editor', encoded => {
        expect(mapUserToForm({ ...USER, birthDate: calendarDate(encoded) }).birthDate).toBeNull();
        expect(new UpdateUserDto({ birthDate: encoded }).birthDate).toBeUndefined();
    });

    it('preserves a local Date selection and an explicit clear', () => {
        const localSelection = new Date('1990-01-02T23:30:00');
        expect(new UpdateUserDto({ birthDate: localSelection }).birthDate?.toISOString()).toBe('1990-01-02T00:00:00.000Z');
        expect(new UpdateUserDto({ birthDate: null }).birthDate).toBeNull();
        expect(new UpdateUserDto({ birthDate: undefined }).birthDate).toBeUndefined();
    });
});

describe('user manage form mapper', () => {
    it('preserves the selected time zone in the profile update without inventing one for legacy users', () => {
        const form = { ...createUserManageFormModel(), ...mapUserToForm({ ...USER, timeZoneId: 'Asia/Tbilisi' }) };
        expect(buildUserUpdateDto(form).timeZoneId).toBe('Asia/Tbilisi');
        const legacy = { ...createUserManageFormModel(), ...mapUserToForm(USER) };
        expect(legacy.timeZoneId).toBeNull();
        expect(buildUserUpdateDto(legacy).timeZoneId).toBeUndefined();
    });
    it('should create user form with empty defaults', () => {
        const model = createUserManageFormModel();

        expect(model.email).toBe('');
        expect(model.username).toBeNull();
        expect(model.profileImage).toBeNull();
    });

    it('should create dietologist form with default shared permissions', () => {
        const model = createDietologistFormModel();

        expect(model.email).toBe('');
        expect(model.shareProfile).toBe(true);
        expect(model.shareMeals).toBe(true);
        expect(model.shareFasting).toBe(true);
    });

    it('should normalize legacy user values into form patch', () => {
        expect(mapUserToForm(USER)).toEqual({
            timeZoneId: null,
            email: USER.email,
            username: USER.username,
            firstName: USER.firstName,
            lastName: USER.lastName,
            gender: Gender.Male,
            language: 'ru',
            theme: 'ocean',
            uiStyle: 'classic',
            birthDate: '1990-05-10',
            heightCm: USER.heightCm,
            activityLevel: USER.activityLevel,
            stepGoal: USER.stepGoal,
            profileImage: imageSelection(USER.profileImage, USER.profileImageAssetId),
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

describe('user manage DOM value mapper', () => {
    it('should normalize optional DOM input values', () => {
        expect(normalizeOptionalTextInput('Alex')).toBe('Alex');
        expect(normalizeOptionalTextInput('')).toBeNull();
        expect(parseOptionalNumberInput(' 180 ')).toBe(EXPECTED_HEIGHT);
        expect(parseOptionalNumberInput('')).toBeNull();
        expect(parseOptionalNumberInput('not-a-number')).toBeNull();
    });

    it('should map the complete form model to the update DTO', () => {
        const form = {
            ...createUserManageFormModel(),
            email: USER.email,
            username: USER.username ?? null,
            profileImage: imageSelection(USER.profileImage ?? '', USER.profileImageAssetId ?? null),
        };

        expect(buildUserUpdateDto(form)).toMatchObject({
            username: USER.username,
            profileImage: USER.profileImage,
            profileImageAssetId: USER.profileImageAssetId,
        });
    });
});
