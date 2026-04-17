import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class TokenStorageService {
    private readonly localStorageRef = typeof localStorage === 'undefined' ? null : localStorage;
    private readonly sessionStorageRef = typeof sessionStorage === 'undefined' ? null : sessionStorage;

    public getToken(): string | null {
        return this.localStorageRef?.getItem('authToken') || this.sessionStorageRef?.getItem('authToken') || null;
    }

    public setToken(token: string, rememberMe?: boolean): void {
        if (rememberMe !== undefined) {
            if (rememberMe) {
                this.localStorageRef?.setItem('authToken', token);
                this.sessionStorageRef?.removeItem('authToken');
            } else {
                this.sessionStorageRef?.setItem('authToken', token);
                this.localStorageRef?.removeItem('authToken');
            }
        } else {
            if (this.localStorageRef?.getItem('authToken') !== null) {
                this.localStorageRef?.setItem('authToken', token);
            } else {
                this.sessionStorageRef?.setItem('authToken', token);
            }
        }
    }

    public clearToken(): void {
        this.localStorageRef?.removeItem('authToken');
        this.sessionStorageRef?.removeItem('authToken');
    }

    public getRefreshToken(): string | null {
        const token = this.localStorageRef?.getItem('refreshToken') ?? null;
        if (!token || token === 'undefined' || token === 'null') {
            this.clearRefreshToken();
            return null;
        }
        return token;
    }

    public setRefreshToken(token: string | null | undefined): void {
        if (!token) {
            this.clearRefreshToken();
            return;
        }
        this.localStorageRef?.setItem('refreshToken', token);
    }

    public clearRefreshToken(): void {
        this.localStorageRef?.removeItem('refreshToken');
    }

    public loadUserId(): string | null {
        const storedId = this.localStorageRef?.getItem('userId') ?? null;
        if (!storedId || storedId === 'undefined') {
            return null;
        }
        return storedId;
    }

    public setUserId(userId: string | null): void {
        if (userId) {
            this.localStorageRef?.setItem('userId', userId);
        } else {
            this.localStorageRef?.removeItem('userId');
        }
    }

    public clearUserId(): void {
        this.localStorageRef?.removeItem('userId');
        this.sessionStorageRef?.removeItem('userId');
    }

    public loadEmailConfirmed(): boolean | null {
        const stored = this.localStorageRef?.getItem('emailConfirmed') ?? null;
        if (stored === 'true') {
            return true;
        }
        if (stored === 'false') {
            return false;
        }
        return null;
    }

    public setEmailConfirmed(value: boolean): void {
        this.localStorageRef?.setItem('emailConfirmed', value ? 'true' : 'false');
    }

    public clearEmailConfirmed(): void {
        this.localStorageRef?.removeItem('emailConfirmed');
    }

    public clearAll(): void {
        this.clearToken();
        this.clearRefreshToken();
        this.clearUserId();
        this.clearEmailConfirmed();
    }
}
