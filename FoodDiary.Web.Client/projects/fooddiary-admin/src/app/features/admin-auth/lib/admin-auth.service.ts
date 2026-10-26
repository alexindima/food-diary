import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject, PLATFORM_ID, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminAuthSdk } from '../../../shared/api/sdk/generated/api/admin-auth.service';
import { AdminSessionSdk } from '../../../shared/api/sdk/generated/api/admin-session.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';

type AdminSession = { id: string; roles: readonly string[] };

@Service()
export class AdminAuthService {
    private readonly sdk = createSdkConnection(AdminAuthSdk, environment.apiUrls.auth, inject(HttpClient), true);
    private readonly sessionSdk = createSdkConnection(AdminSessionSdk, environment.apiUrls.auth, inject(HttpClient), true);
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly session = signal<AdminSession | null>(null);
    private readonly ssoRateLimitedSignal = signal(false);
    public readonly ssoRateLimited = this.ssoRateLimitedSignal.asReadonly();

    public constructor() {
        this.clearLegacyCredentials();
    }

    public getSessionId(): string | null {
        return this.session()?.id ?? null;
    }

    public isAuthenticated(): boolean {
        return this.session() !== null;
    }

    public isAdmin(): boolean {
        return this.session()?.roles.includes('Admin') === true;
    }

    public invalidateSession(): void {
        this.session.set(null);
    }

    public async restoreSessionAsync(force = false): Promise<void> {
        if (!this.isBrowser || (!force && this.session() !== null)) {
            return;
        }
        try {
            this.applySession(await firstValueFrom(this.sessionSdk.client.postAuthSession({ version: this.sessionSdk.version })));
        } catch {
            this.session.set(null);
        }
    }

    public async applySsoFromQueryAsync(): Promise<void> {
        if (!this.isBrowser) {
            return;
        }
        const params = new URLSearchParams(this.document.location.search);
        const fragments = new URLSearchParams(this.document.location.hash.replace(/^#/, ''));
        const code = fragments.get('code') ?? params.get('code');
        if (code === null || code.length === 0) {
            await this.restoreSessionAsync();
            return;
        }
        const success = await this.exchangeSsoCodeAsync(code);
        params.delete('code');
        if (!success) {
            params.set('ssoError', '1');
        }
        this.replaceCurrentUrl(params);
    }

    public async tryApplySsoFromReturnUrlAsync(returnUrl: string): Promise<string | null> {
        let url: URL;
        try {
            url = new URL(returnUrl, this.document.location.origin);
        } catch {
            return null;
        }
        const fragments = new URLSearchParams(url.hash.replace(/^#/, ''));
        const code = fragments.get('code') ?? url.searchParams.get('code');
        if (code === null || code.length === 0) {
            return null;
        }
        const success = await this.exchangeSsoCodeAsync(code);
        url.searchParams.delete('code');
        fragments.delete('code');
        url.hash = fragments.toString();
        return success ? url.pathname + url.search + url.hash : null;
    }

    public async tryUpgradeToAdminAsync(): Promise<boolean> {
        await this.restoreSessionAsync(true);
        return this.isAdmin();
    }

    public async exchangeSsoCodeAsync(code: string): Promise<boolean> {
        this.ssoRateLimitedSignal.set(false);
        try {
            this.applySession(
                await firstValueFrom(
                    this.sdk.client.postAuthAdminSsoExchange({
                        version: this.sdk.version,
                        adminSsoExchangeHttpRequest: { code },
                    }),
                ),
            );
            this.clearLegacyCredentials();
            return this.isAdmin();
        } catch (error: unknown) {
            this.session.set(null);
            this.ssoRateLimitedSignal.set(error instanceof HttpErrorResponse && error.status === Number(HttpStatusCode.TooManyRequests));
            return false;
        }
    }

    private applySession(value: { sessionId?: string | null; roles?: string[] | null }): void {
        if (value.sessionId === null || value.sessionId === undefined || value.sessionId.length === 0 || !Array.isArray(value.roles)) {
            throw new Error('Missing administrative session metadata');
        }
        this.session.set({ id: value.sessionId, roles: value.roles });
    }

    private replaceCurrentUrl(params: URLSearchParams): void {
        const query = params.toString();
        this.document.defaultView?.history.replaceState({}, '', this.document.location.pathname + (query.length > 0 ? `?${query}` : ''));
    }

    private clearLegacyCredentials(): void {
        if (!this.isBrowser) {
            return;
        }
        for (const name of ['localStorage', 'sessionStorage'] as const) {
            try {
                this.document.defaultView?.[name].removeItem('authToken');
                this.document.defaultView?.[name].removeItem('refreshToken');
            } catch {
                // Optional browser storage must not prevent cookie authentication.
            }
        }
    }
}
