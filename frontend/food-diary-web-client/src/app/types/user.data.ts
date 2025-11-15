import { UserFormValues } from '../components/user-manage/user-manage.component';

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
    height?: number;
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
    public weight?: number;
    public height?: number;
    public profileImage?: string;
    public isActive?: boolean;

    public constructor(formValues: Partial<UserFormValues>) {
        this.username = normalizeString(formValues.username);
        this.firstName = normalizeString(formValues.firstName);
        this.lastName = normalizeString(formValues.lastName);
        this.birthDate = formValues.birthDate ? formValues.birthDate.toUtcNativeDate() : undefined;
        this.gender = normalizeString(formValues.gender);
        this.weight = formValues.weight ?? undefined;
        this.height = formValues.height ?? undefined;
        this.profileImage = normalizeString(formValues.profileImage);
        this.isActive = true;
    }
}

const normalizeString = (value: string | null | undefined): string | undefined => {
    const trimmed = value?.trim();
    return trimmed ? trimmed : undefined;
};

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export interface DesiredWeightResponse {
    desiredWeight: number | null;
}

export enum Gender {
    Male = 'M',
    Female = 'F',
    Other = 'O',
}
