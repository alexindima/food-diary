import { UserFormValues } from '../components/user-manage/user-manage.component';

export type ActivityLevelOption = 'MINIMAL' | 'LIGHT' | 'MODERATE' | 'HIGH' | 'EXTREME';

export interface User {
    id: string; // User ID (Guid)
    email: string;
    username?: string;
    firstName?: string;
    lastName?: string;
    birthDate?: Date;
    gender?: string;
    weight?: number;
    desiredWeight?: number;
    desiredWaist?: number;
    height?: number;
    activityLevel?: ActivityLevelOption;
    dailyCalorieTarget?: number;
    proteinTarget?: number;
    fatTarget?: number;
    carbTarget?: number;
    fiberTarget?: number;
    stepGoal?: number;
    waterGoal?: number;
    profileImage?: string;
    isActive: boolean;
    calories?: number; // Local field, not from backend
}

export class UpdateUserDto {
    public username?: string;
    public firstName?: string;
    public lastName?: string;
    public birthDate?: Date;
    public gender?: string;
    public height?: number;
    public activityLevel?: string;
    public dailyCalorieTarget?: number;
    public proteinTarget?: number;
    public fatTarget?: number;
    public carbTarget?: number;
    public stepGoal?: number;
    public waterGoal?: number;
    public profileImage?: string;
    public isActive?: boolean;
    public fiberTarget?: number;

    public constructor(formValues: Partial<UserFormValues> & { fiberTarget?: number | null }) {
        this.username = normalizeString(formValues.username);
        this.firstName = normalizeString(formValues.firstName);
        this.lastName = normalizeString(formValues.lastName);
        this.birthDate = normalizeDate(formValues.birthDate);
        this.gender = normalizeString(formValues.gender);
        this.height = normalizeNumber(formValues.height);
        this.activityLevel = normalizeActivityLevel(formValues.activityLevel);
        this.dailyCalorieTarget = normalizeNumber(formValues.dailyCalorieTarget);
        this.proteinTarget = normalizeNumber(formValues.proteinTarget);
        this.fatTarget = normalizeNumber(formValues.fatTarget);
        this.carbTarget = normalizeNumber(formValues.carbTarget);
        this.fiberTarget = normalizeNumber((formValues as any).fiberTarget);
        this.stepGoal = normalizeInteger(formValues.stepGoal);
        this.waterGoal = normalizeNumber(formValues.waterGoal);
        this.profileImage = normalizeString(formValues.profileImage);
        this.isActive = true;
    }
}

const normalizeString = (value: string | null | undefined): string | undefined => {
    const trimmed = value?.trim();
    return trimmed ? trimmed : undefined;
};

const normalizeDate = (value: Date | null | undefined): Date | undefined => {
    if (!value) {
        return undefined;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return undefined;
    }

    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
};

const normalizeNumber = (value: number | null | undefined): number | undefined =>
    value === null || value === undefined || Number.isNaN(Number(value)) ? undefined : Number(value);

const normalizeInteger = (value: number | null | undefined): number | undefined => {
    const normalized = normalizeNumber(value);
    return normalized === undefined ? undefined : Math.round(normalized);
};

const normalizeActivityLevel = (value: ActivityLevelOption | null | undefined): string | undefined => {
    if (!value) {
        return undefined;
    }

    const lower = value.toLowerCase();
    return lower.charAt(0).toUpperCase() + lower.slice(1);
};

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export interface DesiredWeightResponse {
    desiredWeight: number | null;
}

export interface DesiredWaistResponse {
    desiredWaist: number | null;
}

export enum Gender {
    Male = 'M',
    Female = 'F',
    Other = 'O',
}
