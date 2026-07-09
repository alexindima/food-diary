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
        expect(req.request.body).toMatchObject({
            eventType: 'page_landing',
            sessionId: 'fd-session-test',
            landingPath: '/food-diary?utm_source=telegram&utm_medium=social&utm_campaign=launch',
            referrerHost: 't.me',
            utmSource: 'telegram',
            utmMedium: 'social',
            utmCampaign: 'launch',
            buildVersion: environment.buildVersion,
        });
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
        storage.setJson('local', 'fd_marketing_first_touch', { anonymousId: 'existing' });

        service.initialize();

        httpMock.expectNone(`${environment.apiUrls.marketing}/attribution-events`);
        httpMock.verify();
    });

    it('records signup conversion with first-touch attribution', () => {
        const { service, httpMock, storage } = setup();
        storage.setJson('local', 'fd_marketing_first_touch', {
            eventType: 'page_landing',
            timestamp: '2026-07-09T10:00:00.000Z',
            anonymousId: 'fd-anon-existing',
            sessionId: 'fd-session-first',
            landingPath: '/food-diary?utm_source=telegram',
            utmSource: 'telegram',
            utmMedium: 'social',
            utmCampaign: 'launch',
        });

        service.recordSignupCompleted('user-1');

        const req = httpMock.expectOne(`${environment.apiUrls.marketing}/attribution-events`);
        expect(req.request.body).toMatchObject({
            eventType: 'signup_completed',
            userId: 'user-1',
            anonymousId: 'fd-anon-existing',
            sessionId: 'fd-session-test',
            landingPath: '/food-diary?utm_source=telegram',
            utmSource: 'telegram',
            utmMedium: 'social',
            utmCampaign: 'launch',
        });
        req.flush(null);
        httpMock.verify();
    });
});

function setup(overrides: Partial<BrowserWindowMock> = {}): {
    service: MarketingAttributionService;
    httpMock: HttpTestingController;
    storage: BrowserStorageMock;
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
