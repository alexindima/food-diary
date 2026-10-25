import { inject, Service } from '@angular/core';

import { BrowserStorageService } from '../shared/platform/browser-storage.service';
import { JwtDecoderService } from './jwt-decoder.service';

export const ACCOUNT_ACCESS_STORAGE_KEY = 'accountAccess';

@Service()
export class TokenStorageService {
    private readonly storage = inject(BrowserStorageService);
    private readonly jwtDecoder = inject(JwtDecoderService);
    private owner: string | null = null;

    public getToken(): string | null {
        const tabToken = this.storage.getItem('session', 'authToken');
        if (tabToken !== null) {
            return tabToken;
        }
        const legacyToken = this.storage.getItem('local', 'authToken');
        if (legacyToken !== null) {
            const legacyOwner = this.jwtDecoder.extractUserId(legacyToken);
            if (legacyOwner !== null && this.readAccessOwner() === null) {
                this.saveAccountAccess(legacyOwner, this.jwtDecoder.extractRoles(legacyToken), true);
            }
            this.storage.setItem('session', 'authToken', legacyToken);
            this.storage.setItem('session', 'authRemembered', 'true');
            this.storage.removeItem('local', 'authToken');
        }
        return legacyToken;
    }

    public bindOwner(userId: string | null): void {
        this.owner = userId;
    }

    public pinToken(token: string, userId: string): void {
        const remembered = this.isRemembered();
        this.bindOwner(userId);
        this.storage.setItem('session', 'authToken', token);
        this.storage.setItem('session', 'authRemembered', String(remembered));
    }

    public setToken(token: string, rememberMe?: boolean, publishShared = rememberMe !== undefined): void {
        const remembered = rememberMe ?? this.isRemembered();
        this.storage.setItem('session', 'authToken', token);
        this.storage.setItem('session', 'authRemembered', String(remembered));
        // Remember-me belongs to the server's HttpOnly refresh cookie. Origin-wide
        // storage carries invalidation hints, never a newly issued bearer token.
        if (publishShared || this.ownsSharedSession()) {
            this.storage.removeItem('local', 'authToken');
        }
    }

    public isRemembered(): boolean {
        const remembered = this.storage.getItem('session', 'authRemembered');
        return remembered !== null
            ? remembered === 'true'
            : this.storage.getItem('session', 'authToken') === null && this.storage.getItem('local', 'authToken') !== null;
    }

    public clearToken(): void {
        if (this.ownsSharedSession()) {
            this.storage.removeItem('local', 'authToken');
        }
        this.storage.removeItem('session', 'authToken');
    }

    public saveAccountAccess(userId: string, roles: readonly string[], publish = false): void {
        if (publish || this.ownsSharedSession()) {
            this.storage.setItem('local', ACCOUNT_ACCESS_STORAGE_KEY, this.serializeAccountAccess(userId, roles));
        }
    }

    public hasForeignSession(userId: string): boolean {
        const localOwner = this.jwtDecoder.extractUserId(this.storage.getItem('local', 'authToken'));
        return (localOwner !== null && localOwner !== userId) || (this.readAccessOwner() !== null && this.readAccessOwner() !== userId);
    }

    public hasUpdatedAccountAccess(userId: string, roles: readonly string[]): boolean {
        const stored = this.storage.getItem('local', ACCOUNT_ACCESS_STORAGE_KEY);
        if (stored === null) {
            return false;
        }
        try {
            const snapshot: unknown = JSON.parse(stored);
            return (
                Array.isArray(snapshot) &&
                snapshot[0] === userId &&
                snapshot.every(value => typeof value === 'string') &&
                stored !== this.serializeAccountAccess(userId, roles)
            );
        } catch {
            return false;
        }
    }

    private serializeAccountAccess(userId: string, roles: readonly string[]): string {
        return JSON.stringify([userId, ...new Set([...roles].sort())]);
    }

    public hasRefreshSession(): boolean {
        if (this.storage.getItem('local', 'refreshSession') === 'true') {
            return true;
        }

        const token = this.storage.getItem('local', 'refreshToken');
        return token !== null && token !== 'undefined' && token !== 'null' && token.length > 0;
    }

    public hasPublishedSession(): boolean {
        const owner =
            this.jwtDecoder.extractUserId(this.storage.getItem('local', 'authToken')) ??
            this.readAccessOwner() ??
            this.storage.getItem('local', 'userId');
        return this.hasRefreshSession() && owner !== null && owner !== '' && owner !== 'undefined' && owner !== 'null';
    }

    public consumeLegacyRefreshToken(): string | null {
        if (!this.ownsSharedSession()) {
            return null;
        }
        const token = this.storage.getItem('local', 'refreshToken');
        this.storage.removeItem('local', 'refreshToken');
        if (token === null || token === 'undefined' || token === 'null' || token.length === 0) {
            return null;
        }
        return token;
    }

    public markRefreshSession(publish = false): void {
        if (publish || this.ownsSharedSession()) {
            this.storage.removeItem('local', 'refreshToken');
            this.storage.setItem('local', 'refreshSession', 'true');
        }
    }

    public peekLegacyRefreshTokenForBootstrap(): string | null {
        if (
            this.owner !== null ||
            this.jwtDecoder.extractUserId(this.storage.getItem('session', 'authToken')) !== null ||
            this.jwtDecoder.extractUserId(this.storage.getItem('local', 'authToken')) !== null
        ) {
            return null;
        }
        const token = this.storage.getItem('local', 'refreshToken');
        return token !== null && token !== 'undefined' && token !== 'null' && token.length > 0 ? token : null;
    }

    public clearLegacyRefreshTokenIfMatches(token: string): void {
        // eslint-disable-next-line security/detect-possible-timing-attacks -- local concurrency comparison prevents removing a replacement credential; it does not authenticate a request
        if (this.storage.getItem('local', 'refreshToken') === token) {
            this.storage.removeItem('local', 'refreshToken');
        }
    }

    public clearRefreshSession(): void {
        this.storage.removeItem('local', 'refreshToken');
        this.storage.removeItem('local', 'refreshSession');
    }

    public loadUserId(): string | null {
        const storedId = this.storage.getItem('session', 'userId') ?? this.storage.getItem('local', 'userId');
        if (storedId === null || storedId === 'undefined' || storedId.length === 0) {
            return null;
        }
        return storedId;
    }

    public setUserId(userId: string | null): void {
        this.bindOwner(userId);
        if (userId !== null && userId.length > 0) {
            this.writeMetadata('userId', userId);
        } else {
            this.clearUserId();
        }
    }

    public clearUserId(): void {
        if (this.ownsSharedSession()) {
            this.storage.removeItem('local', 'userId');
        }
        this.storage.removeItem('session', 'userId');
    }

    public loadEmailConfirmed(userId = this.owner): boolean | null {
        const stored = this.readMetadata('emailConfirmed', userId);
        if (stored === 'true') {
            return true;
        }
        if (stored === 'false') {
            return false;
        }
        return null;
    }

    public setEmailConfirmed(value: boolean): void {
        this.writeMetadata('emailConfirmed', String(value));
    }

    public clearEmailConfirmed(): void {
        this.clearMetadata('emailConfirmed');
    }

    public loadMustChangePassword(userId = this.owner): boolean | null {
        const stored = this.readMetadata('mustChangePassword', userId);
        if (stored === 'true') {
            return true;
        }
        if (stored === 'false') {
            return false;
        }
        return null;
    }

    public setMustChangePassword(value: boolean): void {
        this.writeMetadata('mustChangePassword', String(value));
    }

    public clearMustChangePassword(): void {
        this.clearMetadata('mustChangePassword');
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
        for (const key of ['authToken', 'authRemembered', 'userId', 'emailConfirmed', 'mustChangePassword']) {
            this.storage.removeItem('session', key);
        }
    }

    private readMetadata(key: string, userId: string | null): string | null {
        if (userId === null) {
            return null;
        }
        if (this.storage.getItem('session', 'userId') === userId) {
            const value = this.storage.getItem('session', key);
            if (value !== null) {
                return value;
            }
        }
        return this.storage.getItem('local', 'userId') === userId ? this.storage.getItem('local', key) : null;
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

    private clearMetadata(key: string): void {
        this.storage.removeItem('session', key);
        if (this.ownsSharedSession()) {
            this.storage.removeItem('local', key);
        }
    }

    private ownsSharedSession(): boolean {
        const localToken = this.storage.getItem('local', 'authToken');
        const localOwner = this.jwtDecoder.extractUserId(localToken);
        const storedOwner = localOwner ?? this.readAccessOwner() ?? this.storage.getItem('local', 'userId');
        return storedOwner === null || storedOwner === this.owner;
    }

    private readAccessOwner(): string | null {
        const value = this.storage.getJson('local', ACCOUNT_ACCESS_STORAGE_KEY);
        return Array.isArray(value) && typeof value[0] === 'string' ? value[0] : null;
    }
}
