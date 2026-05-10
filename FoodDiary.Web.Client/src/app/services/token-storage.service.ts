import { inject, Injectable } from '@angular/core';

import { BrowserStorageService } from './browser-storage.service';

@Injectable({
    providedIn: 'root',
})
export class TokenStorageService {
    private readonly storage = inject(BrowserStorageService);

    public getToken(): string | null {
        return this.storage.getItem('local', 'authToken') ?? this.storage.getItem('session', 'authToken') ?? null;
    }

    public setToken(token: string, rememberMe?: boolean): void {
        if (rememberMe !== undefined) {
            if (rememberMe) {
                this.storage.setItem('local', 'authToken', token);
                this.storage.removeItem('session', 'authToken');
            } else {
                this.storage.setItem('session', 'authToken', token);
                this.storage.removeItem('local', 'authToken');
            }
        } else {
            if (this.storage.getItem('local', 'authToken') !== null) {
                this.storage.setItem('local', 'authToken', token);
            } else {
                this.storage.setItem('session', 'authToken', token);
            }
        }
    }

    public clearToken(): void {
        this.storage.removeItem('local', 'authToken');
        this.storage.removeItem('session', 'authToken');
    }

    public getRefreshToken(): string | null {
        const token = this.storage.getItem('local', 'refreshToken');
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
        this.storage.setItem('local', 'refreshToken', token);
    }

    public clearRefreshToken(): void {
        this.storage.removeItem('local', 'refreshToken');
    }

    public loadUserId(): string | null {
        const storedId = this.storage.getItem('local', 'userId');
        if (!storedId || storedId === 'undefined') {
            return null;
        }
        return storedId;
    }

    public setUserId(userId: string | null): void {
        if (userId) {
            this.storage.setItem('local', 'userId', userId);
        } else {
            this.storage.removeItem('local', 'userId');
        }
    }

    public clearUserId(): void {
        this.storage.removeItem('local', 'userId');
        this.storage.removeItem('session', 'userId');
    }

    public loadEmailConfirmed(): boolean | null {
        const stored = this.storage.getItem('local', 'emailConfirmed');
        if (stored === 'true') {
            return true;
        }
        if (stored === 'false') {
            return false;
        }
        return null;
    }

    public setEmailConfirmed(value: boolean): void {
        this.storage.setItem('local', 'emailConfirmed', value ? 'true' : 'false');
    }

    public clearEmailConfirmed(): void {
        this.storage.removeItem('local', 'emailConfirmed');
    }

    public clearAll(): void {
        this.clearToken();
        this.clearRefreshToken();
        this.clearUserId();
        this.clearEmailConfirmed();
    }
}
