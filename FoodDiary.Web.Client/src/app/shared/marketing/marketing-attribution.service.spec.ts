import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { SKIP_AUTH } from '../../constants/http-context.tokens';
import { SKIP_OBSERVABILITY } from '../../constants/observability-context.tokens';
import { ClientTelemetrySessionService } from '../observability/client-telemetry-session.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { MarketingAttributionService } from './marketing-attribution.service';

describe('MarketingAttributionService', () => {
    it('captures UTM landing attribution once per session', () => {
        const { service, httpMock, storage } = setup();

        service.initialize();

        const req = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events`);
        expect(req.request.method).toBe('POST');
        expect(req.request.context.get(SKIP_AUTH)).toBe(true);
        expect(req.request.context.get(SKIP_OBSERVABILITY)).toBe(true);
        expect(req.request.withCredentials).toBe(false);
        expect(req.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
        expect(req.request.body).toMatchObject({
            sessionId: 'fd-session-test',
            landingPath: '/food-diary?utm_source=telegram&utm_medium=social&utm_campaign=launch',
            referrerHost: 't.me',
            utmSource: 'telegram',
            utmMedium: 'social',
            utmCampaign: 'launch',
            buildVersion: environment.buildVersion,
        });
        expect(req.request.body).not.toHaveProperty('eventType');
        expect(req.request.body).not.toHaveProperty('userId');
        expect(JSON.stringify(req.request.body)).toContain('fd-anon-');
        req.flush(null);

        service.initialize();
        httpMock.expectNone(`${environment.apiUrls.marketing}/attribution-events`);
        expect(storage.getJson('local', 'fd_marketing_first_touch')).not.toBeNull();
        httpMock.verify();
    });

    it('skips capture when no attribution signal exists after first touch is stored', () => {
        const { service, httpMock, storage } = setup({
            href: 'https://fooddiary.club/dashboard',
            search: '',
            pathname: '/dashboard',
            referrer: '',
        });
        storage.setJson('local', 'fd_marketing_first_touch', { anonymousId: 'existing', sessionId: 'existing-session', landingPath: '/' });

        service.initialize();

        httpMock.expectNone(`${environment.apiUrls.marketing}/attribution-events`);
        httpMock.verify();
    });

    it('records signup conversion with first-touch attribution', () => {
        const { service, httpMock, storage } = setup();
        storage.setJson('local', 'fd_marketing_first_touch', {
            timestamp: '2026-07-09T10:00:00.000Z',
            anonymousId: 'fd-anon-existing',
            sessionId: 'fd-session-first',
            landingPath: '/food-diary?utm_source=telegram',
            utmSource: 'telegram',
            utmMedium: 'social',
            utmCampaign: 'launch',
        });

        service.recordSignupCompleted();

        const req = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events/signup`);
        expect(req.request.context.get(SKIP_AUTH)).toBe(false);
        expect(req.request.context.get(SKIP_OBSERVABILITY)).toBe(true);
        expect(req.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
        expect(req.request.body).toMatchObject({
            anonymousId: 'fd-anon-existing',
            sessionId: 'fd-session-first',
            landingPath: '/food-diary?utm_source=telegram',
            utmSource: 'telegram',
            utmMedium: 'social',
            utmCampaign: 'launch',
        });
        expect(req.request.body).not.toHaveProperty('eventType');
        expect(req.request.body).not.toHaveProperty('userId');
        req.flush(null);
        httpMock.verify();
    });
});

describe('MarketingAttributionService identity lifetimes', () => {
    it('retains the local visitor across a new session and keeps signup on the first touch', () => {
        const { service, httpMock, storage, session } = setup();
        storage.setItem('local', 'fd_marketing_anonymous_id', 'opaque/visitor');
        service.initialize();
        const first = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events`);
        expect(first.request.body).toMatchObject({ anonymousId: 'opaque/visitor', sessionId: 'fd-session-test' });
        first.flush(null);
        storage.removeItem('session', 'fd_marketing_session_capture');
        session.getSessionId = (): string => 'opaque:new-session';

        service.initialize();
        const second = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events`);
        expect(second.request.body).toMatchObject({ anonymousId: 'opaque/visitor', sessionId: 'opaque:new-session' });
        second.flush(null);
        service.recordSignupCompleted();
        const signup = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events/signup`);
        expect(signup.request.body).toMatchObject({ anonymousId: 'opaque/visitor', sessionId: 'fd-session-test' });
        signup.flush(null);
        httpMock.verify();
    });

    it('decodes legacy first-touch strings without a timestamp or UUID format', () => {
        const { service, httpMock, storage } = setup();
        storage.setJson('local', 'fd_marketing_first_touch', {
            anonymousId: 'legacy opaque visitor',
            sessionId: 'legacy/session',
            landingPath: '/legacy',
            utmSource: 'legacy',
        });
        service.recordSignupCompleted();
        const signup = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events/signup`);
        expect(signup.request.body).toMatchObject({
            anonymousId: 'legacy opaque visitor',
            sessionId: 'legacy/session',
            landingPath: '/legacy',
            utmSource: 'legacy',
        });
        const body = signup.request.body as { timestamp: string };
        expect(Number.isNaN(Date.parse(body.timestamp))).toBe(false);
        signup.flush(null);
        httpMock.verify();
    });
});

describe('MarketingAttributionService equivalent landing URLs', () => {
    it.each([
        [
            'utm_campaign=spring+sale&utm_term=x%2By+%26+z',
            'utm_campaign=spring%20sale&utm_term=x%2By%20%26%20z',
            '/food-diary?utm_campaign=spring+sale&utm_term=x%2By+%26+z',
        ],
        [
            'utm_campaign=launch&extra=one+two&extra=three?four',
            'utm_campaign=launch&extra=one%20two&extra=three%3Ffour',
            '/food-diary?utm_campaign=launch',
        ],
    ])('does not recapture equivalent query encoding after reload', (original, reencoded, expectedLanding) => {
        const { service, httpMock, storage, browserWindow } = setup({
            href: `https://fooddiary.club/food-diary?${original}`,
            search: `?${original}`,
        });
        service.initialize();
        const first = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events`);
        expect(first.request.body).toMatchObject({ landingPath: expectedLanding });
        first.flush(null);
        browserWindow.href = `https://fooddiary.club/food-diary?${reencoded}`;
        browserWindow.search = `?${reencoded}`;

        service.initialize();

        httpMock.expectNone(`${environment.apiUrls.marketing}/attribution-events`);
        expect(storage.getJson('local', 'fd_marketing_first_touch')).toMatchObject({ landingPath: expectedLanding });
        httpMock.verify();
    });

    it('recognizes a legacy raw capture key after equivalent re-encoding', () => {
        const { service, httpMock, storage } = setup({
            href: 'https://fooddiary.club/food-diary?utm_campaign=spring+sale',
            search: '?utm_campaign=spring+sale',
        });
        storage.setItem('session', 'fd_marketing_session_capture', '/food-diary?utm_campaign=spring%20sale|t.me|||spring sale||');

        service.initialize();

        httpMock.expectNone(`${environment.apiUrls.marketing}/attribution-events`);
        httpMock.verify();
    });

    it('keeps a literal plus distinct from a query space', () => {
        const { service, httpMock, browserWindow } = setup({
            href: 'https://fooddiary.club/food-diary?utm_campaign=spring%2Bsale',
            search: '?utm_campaign=spring%2Bsale',
        });
        service.initialize();
        const first = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events`);
        expect(first.request.body).toMatchObject({ utmCampaign: 'spring+sale' });
        first.flush(null);
        browserWindow.href = 'https://fooddiary.club/food-diary?utm_campaign=spring+sale';
        browserWindow.search = '?utm_campaign=spring+sale';

        service.initialize();

        const second = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events`);
        expect(second.request.body).toMatchObject({ utmCampaign: 'spring sale' });
        second.flush(null);
        httpMock.verify();
    });
});

describe('MarketingAttributionService URL privacy', () => {
    it.each(['/verify-email', '/reset-password', '/auth/telegram', '/auth/telegram/callback', '/mobile/login'])(
        'sanitizes %s before either browser store or request',
        pathname => {
            const search =
                '?token=verification-secret&code=oauth-secret&state=oauth-state&returnUrl=%2Fverify-email%3Ftoken%3Dnested&utm_source=telegram';
            const { service, httpMock, storage } = setup({ href: `https://fooddiary.club${pathname}${search}`, pathname, search });

            service.initialize();

            const request = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events`);
            expect(request.request.body).toMatchObject({
                landingPath: `${pathname}?utm_source=telegram`,
                utmSource: 'telegram',
                referrerHost: 't.me',
            });
            expect(storage.getJson('local', 'fd_marketing_first_touch')).toMatchObject({ landingPath: `${pathname}?utm_source=telegram` });
            for (const value of [
                JSON.stringify(request.request.body),
                storage.getItem('local', 'fd_marketing_first_touch'),
                storage.getItem('session', 'fd_marketing_session_capture'),
            ]) {
                expect(value).not.toMatch(/verification-secret|oauth-secret|oauth-state|nested/u);
            }
            request.flush(null);
            httpMock.verify();
        },
    );
});

describe('MarketingAttributionService historical URL privacy', () => {
    it('scrubs historical browser entries even when capture is skipped without attribution signals', () => {
        const { service, httpMock, storage } = setup({
            href: 'https://fooddiary.club/dashboard',
            pathname: '/dashboard',
            search: '',
            referrer: '',
        });
        storage.setJson('local', 'fd_marketing_first_touch', {
            anonymousId: 'legacy-visitor',
            sessionId: 'legacy-session',
            landingPath: '/verify-email?token=old-secret&utm_source=telegram',
            utmSource: 'telegram',
        });
        storage.setItem('session', 'fd_marketing_session_capture', '/auth/telegram/callback?code=old-code&state=old-state||||||');

        service.initialize();

        httpMock.expectNone(`${environment.apiUrls.marketing}/attribution-events`);
        expect(storage.getJson('local', 'fd_marketing_first_touch')).toMatchObject({
            landingPath: '/verify-email?utm_source=telegram',
            anonymousId: 'legacy-visitor',
        });
        expect(storage.getItem('session', 'fd_marketing_session_capture')).toBe('["/auth/telegram/callback","","","","","",""]');
        httpMock.verify();
    });

    it('sanitizes historical first touch before signup and removes ambiguous historical session keys', () => {
        const { service, httpMock, storage } = setup();
        storage.setJson('local', 'fd_marketing_first_touch', {
            anonymousId: 'legacy-visitor',
            sessionId: 'legacy-session',
            landingPath: '/verify-email?%74oken=old-secret&utm_source=telegram',
            utmSource: 'telegram',
        });
        storage.setItem('session', 'fd_marketing_session_capture', '/verify-email?token=old|secret|t.me|||||');

        service.recordSignupCompleted();

        const request = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events/signup`);
        expect(request.request.body).toMatchObject({
            landingPath: '/verify-email?utm_source=telegram',
            anonymousId: 'legacy-visitor',
            sessionId: 'legacy-session',
        });
        expect(storage.getItem('local', 'fd_marketing_first_touch')).not.toContain('old-secret');
        expect(storage.getItem('session', 'fd_marketing_session_capture')).toBeNull();
        request.flush(null);
        httpMock.verify();
    });

    it('scrubs a historical raw key before returning for an already captured landing', () => {
        const { service, httpMock, storage } = setup();
        storage.setItem(
            'session',
            'fd_marketing_session_capture',
            '/food-diary?token=old-secret&utm_source=telegram&utm_medium=social&utm_campaign=launch|t.me|telegram|social|launch||',
        );

        service.initialize();

        httpMock.expectNone(`${environment.apiUrls.marketing}/attribution-events`);
        expect(storage.getItem('session', 'fd_marketing_session_capture')).not.toContain('old-secret');
        httpMock.verify();
    });
});

function setup(overrides: Partial<BrowserWindowMock> = {}): {
    service: MarketingAttributionService;
    httpMock: HttpTestingController;
    storage: BrowserStorageMock;
    session: { getSessionId: () => string };
    browserWindow: BrowserWindowMock;
} {
    const storage = new BrowserStorageMock();
    const browserWindow = new BrowserWindowMock(overrides);
    const session = {
        getSessionId: (): string => 'fd-session-test',
    };

    TestBed.configureTestingModule({
        providers: [
            MarketingAttributionService,
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: BrowserStorageService, useValue: storage },
            { provide: BrowserWindowService, useValue: browserWindow },
            { provide: ClientTelemetrySessionService, useValue: session },
        ],
    });

    return {
        service: TestBed.inject(MarketingAttributionService),
        httpMock: TestBed.inject(HttpTestingController),
        storage,
        session,
        browserWindow,
    };
}

class BrowserStorageMock {
    private readonly local = new Map<string, string>();
    private readonly session = new Map<string, string>();

    public getItem(scope: 'local' | 'session', key: string): string | null {
        return this.getMap(scope).get(key) ?? null;
    }

    public setItem(scope: 'local' | 'session', key: string, value: string): void {
        this.getMap(scope).set(key, value);
    }

    public removeItem(scope: 'local' | 'session', key: string): void {
        this.getMap(scope).delete(key);
    }

    public getJson(scope: 'local' | 'session', key: string): unknown {
        const value = this.getItem(scope, key);
        return value === null ? null : (JSON.parse(value) as unknown);
    }

    public setJson(scope: 'local' | 'session', key: string, value: unknown): void {
        this.setItem(scope, key, JSON.stringify(value));
    }

    private getMap(scope: 'local' | 'session'): Map<string, string> {
        return scope === 'local' ? this.local : this.session;
    }
}

class BrowserWindowMock {
    public href = 'https://fooddiary.club/food-diary?utm_source=telegram&utm_medium=social&utm_campaign=launch';
    public search = '?utm_source=telegram&utm_medium=social&utm_campaign=launch';
    public pathname = '/food-diary';
    public hostname = 'fooddiary.club';
    public referrer = 'https://t.me/fooddiary';
    public available = true;

    public constructor(overrides: Partial<BrowserWindowMock>) {
        Object.assign(this, overrides);
    }

    public isAvailable(): boolean {
        return this.available;
    }

    public getHref(): string | null {
        return this.href;
    }

    public getSearch(): string | null {
        return this.search;
    }

    public getPathname(): string | null {
        return this.pathname;
    }

    public getHostname(): string | null {
        return this.hostname;
    }

    public getReferrer(): string | null {
        return this.referrer.length > 0 ? this.referrer : null;
    }
}
