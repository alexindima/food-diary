import { inject, Service } from '@angular/core';

import { BrowserStorageService } from '../shared/platform/browser-storage.service';

@Service()
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
            return;
        }

        if (this.storage.getItem('local', 'authToken') !== null) {
            this.storage.setItem('local', 'authToken', token);
        } else {
            this.storage.setItem('session', 'authToken', token);
        }
    }

    public clearToken(): void {
        this.storage.removeItem('local', 'authToken');
        this.storage.removeItem('session', 'authToken');
    }

    public hasRefreshSession(): boolean {
        if (this.storage.getItem('local', 'refreshSession') === 'true') {
            return true;
        }

        const token = this.storage.getItem('local', 'refreshToken');
        return token !== null && token !== 'undefined' && token !== 'null' && token.length > 0;
    }

    public consumeLegacyRefreshToken(): string | null {
        const token = this.storage.getItem('local', 'refreshToken');
        this.storage.removeItem('local', 'refreshToken');
        if (token === null || token === 'undefined' || token === 'null' || token.length === 0) {
            return null;
        }
        return token;
    }

    public markRefreshSession(): void {
        this.storage.removeItem('local', 'refreshToken');
        this.storage.setItem('local', 'refreshSession', 'true');
    }

    public clearRefreshSession(): void {
        this.storage.removeItem('local', 'refreshToken');
        this.storage.removeItem('local', 'refreshSession');
    }

    public loadUserId(): string | null {
        const storedId = this.storage.getItem('local', 'userId');
        if (storedId === null || storedId === 'undefined' || storedId.length === 0) {
            return null;
        }
        return storedId;
    }

    public setUserId(userId: string | null): void {
        if (userId !== null && userId.length > 0) {
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

    public loadMustChangePassword(): boolean | null {
        const stored = this.storage.getItem('local', 'mustChangePassword');
        if (stored === 'true') {
            return true;
        }
        if (stored === 'false') {
            return false;
        }
        return null;
    }

    public setMustChangePassword(value: boolean): void {
        this.storage.setItem('local', 'mustChangePassword', value ? 'true' : 'false');
    }

    public clearMustChangePassword(): void {
        this.storage.removeItem('local', 'mustChangePassword');
    }

    public clearAll(): void {
        this.clearToken();
        this.clearRefreshSession();
        this.clearUserId();
        this.clearEmailConfirmed();
        this.clearMustChangePassword();
    }
}
