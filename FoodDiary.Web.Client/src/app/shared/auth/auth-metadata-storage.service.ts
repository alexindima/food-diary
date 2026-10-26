import { inject, Service } from '@angular/core';

import { BrowserStorageService } from '../platform/browser-storage.service';

export const ACCOUNT_ACCESS_STORAGE_KEY = 'accountAccess';

/** Non-secret cross-tab hints. Authentication and permissions always come from the API. */
@Service()
export class AuthMetadataStorageService {
    private readonly storage = inject(BrowserStorageService);
    private owner: string | null = null;

    public constructor() {
        for (const area of ['local', 'session'] as const) {
            this.storage.removeItem(area, 'authToken');
            this.storage.removeItem(area, 'refreshToken');
        }
    }

    public bindOwner(userId: string | null): void {
        this.owner = userId;
    }

    public isRemembered(): boolean {
        return this.storage.getItem('session', 'authRemembered') === 'true';
    }

    public setRemembered(value: boolean): void {
        this.storage.setItem('session', 'authRemembered', String(value));
    }

    public saveAccountAccess(userId: string, roles: readonly string[], publish = false): void {
        if (publish || this.ownsSharedSession()) {
            this.storage.setItem('local', ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify([userId, ...new Set([...roles].sort())]));
            this.writeMetadata('userId', userId);
        }
    }

    public hasForeignSession(userId: string): boolean {
        const shared = this.sharedOwner();
        return shared !== null && shared !== userId;
    }

    public hasPublishedSession(): boolean {
        return this.sharedOwner() !== null;
    }

    public setUserId(userId: string | null): void {
        this.bindOwner(userId);
        if (userId !== null) {
            this.writeMetadata('userId', userId);
        }
    }

    public setEmailConfirmed(value: boolean): void {
        this.writeMetadata('emailConfirmed', String(value));
    }

    public setMustChangePassword(value: boolean): void {
        this.writeMetadata('mustChangePassword', String(value));
    }

    public clearAll(): void {
        const ownsShared = this.ownsSharedSession();
        this.clearTabSession();
        if (ownsShared) {
            for (const key of [
                'authToken',
                'refreshToken',
                'refreshSession',
                ACCOUNT_ACCESS_STORAGE_KEY,
                'userId',
                'emailConfirmed',
                'mustChangePassword',
            ]) {
                this.storage.removeItem('local', key);
            }
        }
    }

    public clearTabSession(): void {
        for (const key of ['authToken', 'refreshToken', 'authRemembered', 'userId', 'emailConfirmed', 'mustChangePassword']) {
            this.storage.removeItem('session', key);
        }
    }

    private writeMetadata(key: string, value: string): void {
        if (this.owner === null) {
            return;
        }
        this.storage.setItem('session', key, value);
        if (this.ownsSharedSession()) {
            this.storage.setItem('local', key, value);
        }
    }

    private ownsSharedSession(): boolean {
        const shared = this.sharedOwner();
        return shared === null || shared === this.owner;
    }

    private sharedOwner(): string | null {
        const access = this.storage.getJson('local', ACCOUNT_ACCESS_STORAGE_KEY);
        const value = Array.isArray(access) && typeof access[0] === 'string' ? access[0] : this.storage.getItem('local', 'userId');
        return value !== null && value !== '' && value !== 'undefined' && value !== 'null' ? value : null;
    }
}
