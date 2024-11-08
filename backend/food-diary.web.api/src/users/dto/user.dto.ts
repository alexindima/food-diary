export class UserDto {
    email: string;
    username: string;
    firstName?: string;
    lastName?: string;
    birthDate?: Date;
    gender?: string;
    weight?: number;
    height?: number;
    profileImage?: string;
    isActive: boolean;

    constructor(partial: Partial<UserDto>) {
        Object.assign(this, partial);
    }
}
