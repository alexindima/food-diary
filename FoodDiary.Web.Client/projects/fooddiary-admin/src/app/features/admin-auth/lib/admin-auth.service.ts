import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';

const BASE64_BLOCK_SIZE = 4;
const BASE64_REMAINDER_NONE = 0;

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
    private readonly authUrl = environment.apiUrls.auth;
    private readonly http = inject(HttpClient);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private readonly tokenSignal = signal<string | null>(this.getToken());

    public getToken(): string | null {
        return this.localStorageRef?.getItem('authToken') ?? this.sessionStorageRef?.getItem('authToken') ?? null;
    }

    public isAuthenticated(): boolean {
        return Boolean(this.tokenSignal());
    }

    public isAdmin(): boolean {
        const token = this.tokenSignal();
        if (token === null || token.length === 0) {
            return false;
        }

        const roles = this.extractRolesFromToken(token);
        return roles.includes('Admin');
    }

    public refreshTokenState(): void {
        this.captureTokenFromQuery();
        this.tokenSignal.set(this.getToken());
    }

    public async applySsoFromQueryAsync(): Promise<void> {
        if (!this.isBrowser) {
            return;
        }

        const params = new URLSearchParams(this.document.location.search);
        const code = params.get('code');
        if (code === null || code.length === 0) {
            return;
        }

        if (this.isAuthenticated()) {
            this.clearCodeFromUrl(params);
            this.tokenSignal.set(this.getToken());
            return;
        }

        if (this.wasCodeProcessed(code)) {
            this.clearCodeFromUrl(params);
            this.tokenSignal.set(this.getToken());
            return;
        }

        const success = await this.exchangeSsoCodeAsync(code);
        this.markCodeProcessed(code);

        params.delete('code');
        if (!success) {
            params.set('ssoError', '1');
        }

        this.replaceCurrentUrl(params);
        this.tokenSignal.set(this.getToken());
    }

    public async tryApplySsoFromReturnUrlAsync(returnUrl: string): Promise<string | null> {
        const result = this.extractCodeFromUrl(returnUrl);
        if (result === null) {
            return null;
        }

        if (this.isAuthenticated()) {
            return result.cleanedUrl;
        }

        if (this.wasCodeProcessed(result.code)) {
            return result.cleanedUrl;
        }

        const success = await this.exchangeSsoCodeAsync(result.code);
        this.markCodeProcessed(result.code);
        this.tokenSignal.set(this.getToken());
        return success ? result.cleanedUrl : null;
    }

    public async tryUpgradeToAdminAsync(): Promise<boolean> {
        if (this.isAdmin()) {
            return true;
        }

        const token = this.getToken();
        if (token === null || token.length === 0) {
            return false;
        }

        try {
            const response = await firstValueFrom(this.http.post<AdminSsoStartResponse>(`${this.authUrl}/admin-sso/start`, {}));
            if (response.code.length === 0) {
                return false;
            }

            const success = await this.exchangeSsoCodeAsync(response.code);
            this.tokenSignal.set(this.getToken());
            return success;
        } catch {
            return false;
        }
    }

    public async exchangeSsoCodeAsync(code: string): Promise<boolean> {
        try {
            const response = await firstValueFrom(this.http.post<AuthenticationResponse>(`${this.authUrl}/admin-sso/exchange`, { code }));

            if (response.accessToken.length === 0) {
                return false;
            }

            this.localStorageRef?.setItem('authToken', response.accessToken);
            this.localStorageRef?.setItem('refreshToken', response.refreshToken);
            return true;
        } catch {
            return false;
        }
    }

    private captureTokenFromQuery(): void {
        if (!this.isBrowser) {
            return;
        }

        const params = new URLSearchParams(this.document.location.search);
        const token = params.get('authToken') ?? params.get('accessToken');
        if (token === null || token.length === 0) {
            return;
        }

        this.localStorageRef?.setItem('authToken', token);
        this.clearTokenParams(params);
    }

    private clearCodeFromUrl(params: URLSearchParams): void {
        params.delete('code');
        this.replaceCurrentUrl(params);
    }

    private clearTokenParams(params: URLSearchParams): void {
        params.delete('authToken');
        params.delete('accessToken');
        this.replaceCurrentUrl(params);
    }

    private replaceCurrentUrl(params: URLSearchParams): void {
        if (!this.isBrowser) {
            return;
        }

        const nextQuery = params.toString();
        const nextUrl = nextQuery.length > 0 ? `${this.document.location.pathname}?${nextQuery}` : this.document.location.pathname;
        this.document.defaultView?.history.replaceState({}, '', nextUrl);
    }

    private wasCodeProcessed(code: string): boolean {
        return this.sessionStorageRef?.getItem('adminSsoCode') === code;
    }

    private markCodeProcessed(code: string): void {
        this.sessionStorageRef?.setItem('adminSsoCode', code);
    }

    private extractRolesFromToken(token: string): string[] {
        const payload = this.decodePayload(token);
        if (payload === null) {
            return [];
        }

        const roleClaim =
            payload['role'] ??
            payload['roles'] ??
            payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
            payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'];

        if (Array.isArray(roleClaim)) {
            return roleClaim.filter((role): role is string => typeof role === 'string');
        }

        if (typeof roleClaim === 'string') {
            return [roleClaim];
        }

        return [];
    }

    private extractCodeFromUrl(value: string): { code: string; cleanedUrl: string } | null {
        const decoded = this.safeDecode(value);
        if (decoded.length === 0) {
            return null;
        }

        try {
            const url = new URL(decoded, this.document.location.origin);
            const code = url.searchParams.get('code');
            if (code === null || code.length === 0) {
                return null;
            }

            url.searchParams.delete('code');
            const cleanedSearch = url.searchParams.toString();
            const cleanedUrl = cleanedSearch.length > 0 ? `${url.pathname}?${cleanedSearch}` : url.pathname;
            return { code, cleanedUrl };
        } catch {
            return null;
        }
    }

    private safeDecode(value: string): string {
        try {
            return decodeURIComponent(value);
        } catch {
            return value;
        }
    }

    private decodePayload(token: string): Record<string, unknown> | null {
        const tokenSegments = token.split('.');
        if (tokenSegments.length < 2 || tokenSegments[1].length === 0) {
            return null;
        }

        const payloadSegment = tokenSegments[1];
        try {
            const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
            const remainder = normalized.length % BASE64_BLOCK_SIZE;
            const padLength =
                (BASE64_BLOCK_SIZE - (remainder === BASE64_REMAINDER_NONE ? BASE64_BLOCK_SIZE : remainder)) % BASE64_BLOCK_SIZE;
            const padded = normalized.padEnd(normalized.length + padLength, '=');
            const decoded = atob(padded);
            const parsed: unknown = JSON.parse(decoded);
            return this.isRecord(parsed) ? parsed : null;
        } catch {
            return null;
        }
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null && !Array.isArray(value);
    }

    private get localStorageRef(): Storage | null {
        return this.getBrowserStorage('localStorage');
    }

    private get sessionStorageRef(): Storage | null {
        return this.getBrowserStorage('sessionStorage');
    }

    private getBrowserStorage(storageName: 'localStorage' | 'sessionStorage'): Storage | null {
        if (!this.isBrowser) {
            return null;
        }

        try {
            return this.document.defaultView?.[storageName] ?? null;
        } catch {
            return null;
        }
    }
}

type AuthenticationResponse = {
    accessToken: string;
    refreshToken: string;
};

type AdminSsoStartResponse = {
    code: string;
};
