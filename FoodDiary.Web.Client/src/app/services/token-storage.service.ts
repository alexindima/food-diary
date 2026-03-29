import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class TokenStorageService {
    public getToken(): string | null {
        return localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    }

    public setToken(token: string, rememberMe?: boolean): void {
        if (rememberMe !== undefined) {
            if (rememberMe) {
                localStorage.setItem('authToken', token);
                sessionStorage.removeItem('authToken');
            } else {
                sessionStorage.setItem('authToken', token);
                localStorage.removeItem('authToken');
            }
        } else {
            if (localStorage.getItem('authToken') !== null) {
                localStorage.setItem('authToken', token);
            } else {
                sessionStorage.setItem('authToken', token);
            }
        }
    }

    public clearToken(): void {
        localStorage.removeItem('authToken');
        sessionStorage.removeItem('authToken');
    }

    public getRefreshToken(): string | null {
        const token = localStorage.getItem('refreshToken');
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
        localStorage.setItem('refreshToken', token);
    }

    public clearRefreshToken(): void {
        localStorage.removeItem('refreshToken');
    }

    public loadUserId(): string | null {
        const storedId = localStorage.getItem('userId');
        if (!storedId || storedId === 'undefined') {
            return null;
        }
        return storedId;
    }

    public setUserId(userId: string | null): void {
        if (userId) {
            localStorage.setItem('userId', userId);
        } else {
            localStorage.removeItem('userId');
        }
    }

    public clearUserId(): void {
        localStorage.removeItem('userId');
        sessionStorage.removeItem('userId');
    }

    public loadEmailConfirmed(): boolean | null {
        const stored = localStorage.getItem('emailConfirmed');
        if (stored === 'true') {
            return true;
        }
        if (stored === 'false') {
            return false;
        }
        return null;
    }

    public setEmailConfirmed(value: boolean): void {
        localStorage.setItem('emailConfirmed', value ? 'true' : 'false');
    }

    public clearEmailConfirmed(): void {
        localStorage.removeItem('emailConfirmed');
    }

    public clearAll(): void {
        this.clearToken();
        this.clearRefreshToken();
        this.clearUserId();
        this.clearEmailConfirmed();
    }
}
