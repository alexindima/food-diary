import { User } from './user.data';

export type AuthMode = 'login' | 'register';

export class LoginRequest {
    public email: string;
    public password: string;
    public rememberMe: boolean;

    public constructor(formValue: Partial<LoginRequest>) {
        this.email = formValue.email || '';
        this.password = formValue.password || '';
        this.rememberMe = formValue.rememberMe || false;
    }
}

export class RegisterRequest {
    public email: string;
    public password: string;

    public constructor(formValue: Partial<RegisterRequest>) {
        this.email = formValue.email || '';
        this.password = formValue.password || '';
    }
}

export interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    user: User;
}
