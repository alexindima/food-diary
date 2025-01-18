import { UserFormValues } from '../components/user-manage/user-manage.component';

export interface User {
    email: string;
    password?: string;
    username: string;
    firstName?: string;
    lastName?: string;
    birthDate?: Date;
    gender?: string;
    weight?: number;
    height?: number;
    calories?: number;
    profileImage?: string;
    isActive: boolean;
}

export class UpdateUserDto {
    public email?: string;
    public password?: string;
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
        this.password = formValues.password || undefined;
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

export enum Gender {
    Male = 'M',
    Female = 'F',
    Other = 'O',
}
