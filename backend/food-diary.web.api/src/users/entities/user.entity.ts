export class User {
    id: number;
    username: string;
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
    birthDate?: Date;
    gender?: string;
    weight?: number;
    height?: number;
    goal?: string;
    profileImage?: string;
    isActive: boolean;
    createdAt: Date;
    updatedAt: Date;
}
