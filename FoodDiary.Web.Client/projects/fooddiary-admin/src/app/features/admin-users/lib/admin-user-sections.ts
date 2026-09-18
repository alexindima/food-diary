import type { AdminUser } from '../models/admin-user.models';

export type DetailField = {
    label: string;
    value: string;
};

export type DetailSection = {
    title: string;
    fields: DetailField[];
};

// eslint-disable-next-line max-lines-per-function -- Static account field metadata is kept together for review.
export function buildAdminUserSections(currentUser: AdminUser, passwordChangeLabel: string): DetailSection[] {
    return [
        {
            title: 'Account',
            fields: [
                { label: 'User ID', value: currentUser.id },
                { label: 'Email', value: text(currentUser.email) },
                { label: 'Username', value: text(currentUser.username) },
                { label: 'Roles', value: text(currentUser.roles.join(', ')) },
                { label: 'Active', value: boolean(currentUser.isActive) },
                { label: 'Email confirmed', value: boolean(currentUser.isEmailConfirmed) },
                { label: 'Has password', value: boolean(currentUser.hasPassword) },
                { label: passwordChangeLabel, value: boolean(currentUser.mustChangePassword) },
                { label: 'Telegram user ID', value: number(currentUser.telegramUserId) },
            ],
        },
        {
            title: 'Profile',
            fields: [
                { label: 'First name', value: text(currentUser.firstName) },
                { label: 'Last name', value: text(currentUser.lastName) },
                { label: 'Birth date', value: date(currentUser.birthDate) },
                { label: 'Gender', value: text(currentUser.gender) },
                { label: 'Height', value: number(currentUser.heightCm) },
                { label: 'Weight', value: number(currentUser.weightKg) },
                { label: 'Desired weight', value: number(currentUser.desiredWeightKg) },
                { label: 'Desired waist', value: number(currentUser.desiredWaistCm) },
                { label: 'Activity level', value: text(currentUser.activityLevel) },
            ],
        },
        {
            title: 'Goals',
            fields: [
                { label: 'Daily calories', value: number(currentUser.dailyCalorieTarget) },
                { label: 'Protein target', value: number(currentUser.proteinTarget) },
                { label: 'Fat target', value: number(currentUser.fatTarget) },
                { label: 'Carb target', value: number(currentUser.carbTarget) },
                { label: 'Fiber target', value: number(currentUser.fiberTarget) },
                { label: 'Water goal', value: number(currentUser.waterGoal) },
                { label: 'Hydration goal', value: number(currentUser.hydrationGoal) },
                { label: 'Step goal', value: number(currentUser.stepGoal) },
                { label: 'Calorie cycling', value: boolean(currentUser.calorieCyclingEnabled) },
            ],
        },
        {
            title: 'Calorie cycling',
            fields: [
                { label: 'Monday', value: number(currentUser.mondayCalories) },
                { label: 'Tuesday', value: number(currentUser.tuesdayCalories) },
                { label: 'Wednesday', value: number(currentUser.wednesdayCalories) },
                { label: 'Thursday', value: number(currentUser.thursdayCalories) },
                { label: 'Friday', value: number(currentUser.fridayCalories) },
                { label: 'Saturday', value: number(currentUser.saturdayCalories) },
                { label: 'Sunday', value: number(currentUser.sundayCalories) },
            ],
        },
        {
            title: 'Settings',
            fields: [
                { label: 'Language', value: text(currentUser.language) },
                { label: 'Theme', value: text(currentUser.theme) },
                { label: 'UI style', value: text(currentUser.uiStyle) },
                { label: 'Push notifications', value: boolean(currentUser.pushNotificationsEnabled) },
                { label: 'Fasting notifications', value: boolean(currentUser.fastingPushNotificationsEnabled) },
                { label: 'Social notifications', value: boolean(currentUser.socialPushNotificationsEnabled) },
                { label: 'Fasting reminder hours', value: number(currentUser.fastingCheckInReminderHours) },
                { label: 'Fasting follow-up hours', value: number(currentUser.fastingCheckInFollowUpReminderHours) },
                { label: 'Dashboard layout JSON', value: text(currentUser.dashboardLayoutJson) },
            ],
        },
        {
            title: 'AI',
            fields: [
                { label: 'Input token limit', value: number(currentUser.aiInputTokenLimit) },
                { label: 'Output token limit', value: number(currentUser.aiOutputTokenLimit) },
                { label: 'Consent accepted', value: dateTime(currentUser.aiConsentAcceptedAt) },
            ],
        },
        {
            title: 'Audit',
            fields: [
                { label: 'Created', value: dateTime(currentUser.createdOnUtc) },
                { label: 'Deleted', value: dateTime(currentUser.deletedAt) },
                { label: 'Last login', value: dateTime(currentUser.lastLoginAtUtc) },
                { label: 'Profile image asset ID', value: text(currentUser.profileImageAssetId) },
            ],
        },
    ];
}

function text(value: string | null | undefined): string {
    return value !== null && value !== undefined && value.trim().length > 0 ? value : '-';
}

function number(value: number | null | undefined): string {
    return value !== null && value !== undefined ? String(value) : '-';
}

function boolean(value: boolean | null | undefined): string {
    return value === true ? 'Yes' : 'No';
}

function date(value: string | null | undefined): string {
    return value !== null && value !== undefined ? new Date(value).toLocaleDateString() : '-';
}

function dateTime(value: string | null | undefined): string {
    return value !== null && value !== undefined ? new Date(value).toLocaleString() : '-';
}
