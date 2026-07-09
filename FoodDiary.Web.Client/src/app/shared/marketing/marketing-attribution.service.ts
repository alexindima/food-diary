import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Service } from '@angular/core';

import { environment } from '../../../environments/environment';
import { SKIP_AUTH } from '../../constants/http-context.tokens';
import { SKIP_OBSERVABILITY } from '../../constants/observability-context.tokens';
import { ClientTelemetrySessionService } from '../observability/client-telemetry-session.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';

type MarketingAttributionPayload = {
    eventType: 'page_landing' | 'signup_completed' | 'premium_started';
    timestamp: string;
    userId?: string;
    anonymousId: string;
    sessionId: string;
    landingPath: string;
    referrerHost?: string;
    utmSource?: string;
    utmMedium?: string;
    utmCampaign?: string;
    utmContent?: string;
    utmTerm?: string;
    buildVersion?: string;
};

const ANONYMOUS_ID_STORAGE_KEY = 'fd_marketing_anonymous_id';
const FIRST_TOUCH_STORAGE_KEY = 'fd_marketing_first_touch';
const SESSION_CAPTURE_STORAGE_KEY = 'fd_marketing_session_capture';
const ANONYMOUS_ID_PREFIX = 'fd-anon';
const RANDOM_RADIX = 36;

@Service()
export class MarketingAttributionService {
    private readonly http = inject(HttpClient);
    private readonly storage = inject(BrowserStorageService);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly telemetrySession = inject(ClientTelemetrySessionService);
    private readonly baseUrl = `${environment.apiUrls.marketing}/attribution-events`;
    private readonly telemetryContext = new HttpContext().set(SKIP_AUTH, true).set(SKIP_OBSERVABILITY, true);

    public initialize(): void {
        if (!this.browserWindow.isAvailable()) {
            return;
        }

        const payload = this.createPayload();
        if (payload === null || this.wasCapturedInSession(payload)) {
            return;
        }

        this.persistFirstTouch(payload);
        this.markCapturedInSession(payload);
        this.http.post<void>(this.baseUrl, payload, { context: this.telemetryContext }).subscribe({
            error: () => {
                // Attribution failures should never affect app flow.
            },
        });
    }

    public recordSignupCompleted(userId: string): void {
        this.recordConversion('signup_completed', userId);
    }

    public recordPremiumStarted(userId: string): void {
        this.recordConversion('premium_started', userId);
    }

    private createPayload(): MarketingAttributionPayload | null {
        const landingPath = this.getLandingPath();
        const params = new URLSearchParams(this.browserWindow.getSearch() ?? '');
        const referrerHost = this.getExternalReferrerHost();
        const payload: MarketingAttributionPayload = {
            eventType: 'page_landing',
            timestamp: new Date().toISOString(),
            anonymousId: this.getAnonymousId(),
            sessionId: this.telemetrySession.getSessionId(),
            landingPath,
            ...(referrerHost !== null ? { referrerHost } : {}),
            ...this.readUtmParams(params),
            ...(environment.buildVersion !== undefined ? { buildVersion: environment.buildVersion } : {}),
        };

        if (this.hasAttributionSignal(payload) || this.storage.getJson('local', FIRST_TOUCH_STORAGE_KEY) === null) {
            return payload;
        }

        return null;
    }

    private recordConversion(eventType: MarketingAttributionPayload['eventType'], userId: string): void {
        if (!this.browserWindow.isAvailable() || userId.length === 0) {
            return;
        }

        const firstTouch = this.readFirstTouch();
        const payload: MarketingAttributionPayload = {
            ...(firstTouch ?? this.createOrganicPayload()),
            eventType,
            timestamp: new Date().toISOString(),
            userId,
            sessionId: this.telemetrySession.getSessionId(),
        };

        this.http.post<void>(this.baseUrl, payload, { context: this.telemetryContext }).subscribe({
            error: () => {
                // Attribution failures should never affect app flow.
            },
        });
    }

    private createOrganicPayload(): MarketingAttributionPayload {
        return {
            eventType: 'page_landing',
            timestamp: new Date().toISOString(),
            anonymousId: this.getAnonymousId(),
            sessionId: this.telemetrySession.getSessionId(),
            landingPath: this.getLandingPath(),
            ...(environment.buildVersion !== undefined ? { buildVersion: environment.buildVersion } : {}),
        };
    }

    private readFirstTouch(): MarketingAttributionPayload | null {
        const value = this.storage.getJson('local', FIRST_TOUCH_STORAGE_KEY);
        if (!this.isStoredAttributionPayload(value)) {
            return null;
        }

        return value;
    }

    private getLandingPath(): string {
        const href = this.browserWindow.getHref();
        if (href === null) {
            return this.browserWindow.getPathname() ?? '/';
        }

        try {
            const url = new URL(href);
            return `${url.pathname}${url.search}`;
        } catch {
            return this.browserWindow.getPathname() ?? '/';
        }
    }

    private readUtmParams(params: URLSearchParams): Partial<MarketingAttributionPayload> {
        return {
            ...this.readParam(params, 'utm_source', 'utmSource'),
            ...this.readParam(params, 'utm_medium', 'utmMedium'),
            ...this.readParam(params, 'utm_campaign', 'utmCampaign'),
            ...this.readParam(params, 'utm_content', 'utmContent'),
            ...this.readParam(params, 'utm_term', 'utmTerm'),
        };
    }

    private readParam(
        params: URLSearchParams,
        queryKey: string,
        payloadKey: keyof MarketingAttributionPayload,
    ): Partial<MarketingAttributionPayload> {
        const value = params.get(queryKey)?.trim();
        return value === undefined || value.length === 0 ? {} : { [payloadKey]: value };
    }

    private getExternalReferrerHost(): string | null {
        const referrer = this.browserWindow.getReferrer();
        if (referrer === null) {
            return null;
        }

        try {
            const referrerUrl = new URL(referrer);
            const currentHost = this.browserWindow.getHostname();
            return currentHost !== null && referrerUrl.hostname === currentHost ? null : referrerUrl.hostname;
        } catch {
            return null;
        }
    }

    private getAnonymousId(): string {
        const stored = this.storage.getItem('local', ANONYMOUS_ID_STORAGE_KEY);
        if (stored !== null && stored.length > 0) {
            return stored;
        }

        const anonymousId = this.createAnonymousId();
        this.storage.setItem('local', ANONYMOUS_ID_STORAGE_KEY, anonymousId);
        return anonymousId;
    }

    private createAnonymousId(): string {
        const timestamp = Date.now().toString(RANDOM_RADIX);
        const random = Math.random().toString(RANDOM_RADIX).slice(2);
        return `${ANONYMOUS_ID_PREFIX}-${timestamp}-${random}`;
    }

    private persistFirstTouch(payload: MarketingAttributionPayload): void {
        if (this.storage.getJson('local', FIRST_TOUCH_STORAGE_KEY) !== null) {
            return;
        }

        this.storage.setJson('local', FIRST_TOUCH_STORAGE_KEY, payload);
    }

    private wasCapturedInSession(payload: MarketingAttributionPayload): boolean {
        return this.storage.getItem('session', SESSION_CAPTURE_STORAGE_KEY) === this.getCaptureKey(payload);
    }

    private markCapturedInSession(payload: MarketingAttributionPayload): void {
        this.storage.setItem('session', SESSION_CAPTURE_STORAGE_KEY, this.getCaptureKey(payload));
    }

    private getCaptureKey(payload: MarketingAttributionPayload): string {
        return [
            payload.landingPath,
            payload.referrerHost ?? '',
            payload.utmSource ?? '',
            payload.utmMedium ?? '',
            payload.utmCampaign ?? '',
            payload.utmContent ?? '',
            payload.utmTerm ?? '',
        ].join('|');
    }

    private hasAttributionSignal(payload: MarketingAttributionPayload): boolean {
        return (
            payload.referrerHost !== undefined ||
            payload.utmSource !== undefined ||
            payload.utmMedium !== undefined ||
            payload.utmCampaign !== undefined ||
            payload.utmContent !== undefined ||
            payload.utmTerm !== undefined
        );
    }

    private isStoredAttributionPayload(value: unknown): value is MarketingAttributionPayload {
        if (typeof value !== 'object' || value === null || Array.isArray(value)) {
            return false;
        }

        return (
            this.hasStringProperty(value, 'anonymousId') &&
            this.hasStringProperty(value, 'sessionId') &&
            this.hasStringProperty(value, 'landingPath')
        );
    }

    private hasStringProperty(value: object, key: string): boolean {
        return Object.hasOwn(value, key) && typeof Reflect.get(value, key) === 'string';
    }
}
