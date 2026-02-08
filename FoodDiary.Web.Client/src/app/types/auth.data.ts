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
    public language?: string;

    public constructor(formValue: Partial<RegisterRequest>) {
        this.email = formValue.email || '';
        this.password = formValue.password || '';
        this.language = formValue.language;
    }
}

export class RestoreAccountRequest {
    public email: string;
    public password: string;

    public constructor(formValue: Partial<RestoreAccountRequest>) {
        this.email = formValue.email || '';
        this.password = formValue.password || '';
    }
}

export class PasswordResetRequest {
    public email: string;

    public constructor(formValue: Partial<PasswordResetRequest>) {
        this.email = formValue.email || '';
    }
}

export class ConfirmPasswordResetRequest {
    public userId: string;
    public token: string;
    public newPassword: string;

    public constructor(formValue: Partial<ConfirmPasswordResetRequest>) {
        this.userId = formValue.userId || '';
        this.token = formValue.token || '';
        this.newPassword = formValue.newPassword || '';
    }
}

export interface TelegramLoginWidgetRequest {
    id: number;
    authDate: number;
    hash: string;
    username?: string;
    firstName?: string;
    lastName?: string;
    photoUrl?: string;
}

export interface TelegramAuthRequest {
    initData: string;
}

export interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    user: User;
}
