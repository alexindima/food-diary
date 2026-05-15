import type { DietologistPermissions } from '../../../../shared/models/dietologist.data';
import type { ActivityLevelOption } from '../../../../shared/models/user.data';

export type DietologistPermissionOption = {
    controlName: keyof DietologistPermissions;
    labelKey: string;
};

export const DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS = 12;
export const DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS = 20;
export const MAX_FASTING_REMINDER_HOURS = 168;
export const TEST_NOTIFICATION_DELAY_SECONDS = 20;

export const ACTIVITY_LEVEL_OPTIONS: ActivityLevelOption[] = ['MINIMAL', 'LIGHT', 'MODERATE', 'HIGH', 'EXTREME'];
export const LANGUAGE_CODES = ['en', 'ru'];

export const DEFAULT_DIETOLOGIST_PERMISSIONS: DietologistPermissions = {
    shareProfile: true,
    shareMeals: true,
    shareStatistics: true,
    shareWeight: true,
    shareWaist: true,
    shareGoals: true,
    shareHydration: true,
    shareFasting: true,
};

export const DIETOLOGIST_PERMISSION_OPTIONS: DietologistPermissionOption[] = [
    { controlName: 'shareProfile', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_PROFILE' },
    { controlName: 'shareMeals', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_MEALS' },
    { controlName: 'shareStatistics', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_STATISTICS' },
    { controlName: 'shareWeight', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_WEIGHT' },
    { controlName: 'shareWaist', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_WAIST' },
    { controlName: 'shareGoals', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_GOALS' },
    { controlName: 'shareHydration', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_HYDRATION' },
    { controlName: 'shareFasting', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_FASTING' },
];
