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
    height?: number;
    profileImage?: string;
    isActive: boolean;
    calories?: number; // Local field, not from backend
}

export class UpdateUserDto {
    public email?: string;
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
        this.email = formValues.email || undefined;
        this.username = formValues.username || undefined;
        this.firstName = formValues.firstName || undefined;
        this.lastName = formValues.lastName || undefined;
        this.birthDate = formValues.birthDate ? formValues.birthDate.toUtcNativeDate() : undefined;
        this.gender = formValues.gender || undefined;
        this.weight = formValues.weight || undefined;
        this.height = formValues.height || undefined;
        this.profileImage = formValues.profileImage || undefined;
        this.isActive = true;
    }
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export enum Gender {
    Male = 'M',
    Female = 'F',
    Other = 'O',
}
