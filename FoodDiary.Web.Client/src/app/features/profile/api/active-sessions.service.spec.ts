import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { ActiveSessionsService } from './active-sessions.service';

describe('Active sessions generated transport', () => {
    let service: ActiveSessionsService;
    let http: HttpTestingController;
    const base = `${environment.apiUrls.auth}/sessions`;
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(ActiveSessionsService);
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        http.verify();
    });

    it('preserves the current-session flag and nullable device metadata', () => {
        const row = {
            id: 'session',
            isCurrent: true,
            authProvider: null,
            browser: null,
            operatingSystem: null,
            deviceType: null,
            createdAtUtc: '2026-10-06T10:00:00Z',
            lastActiveAtUtc: '2026-10-06T11:00:00Z',
        };
        service.getAll().subscribe(value => {
            expect(value).toEqual([row]);
        });
        const request = http.expectOne(`${base}?page=1&limit=100`);
        expect(request.request.withCredentials).toBe(true);
        request.flush([row]);
    });

    it('keeps separate owner-scoped revocation routes and no-content completion', () => {
        let completed = false;
        service.revoke('session').subscribe(() => {
            completed = true;
        });
        const request = http.expectOne(`${base}/session`);
        expect(request.request.method).toBe('DELETE');
        request.flush(null, { status: 204, statusText: 'No Content' });
        expect(completed).toBe(true);
        service.revokeOthers().subscribe();
        const others = http.expectOne(base);
        expect(others.request.method).toBe('DELETE');
        others.flush(null, { status: 204, statusText: 'No Content' });
    });
});
