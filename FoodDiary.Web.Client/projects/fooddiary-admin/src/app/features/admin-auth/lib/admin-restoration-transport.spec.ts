import { HttpStatusCode, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { adminAuthInterceptor } from './admin-auth.interceptor';
import { AdminAuthService } from './admin-auth.service';

const base = environment.apiUrls.auth;
const response = { sessionId: 'admin-current', roles: ['Admin'], user: { id: 'admin' } };
const cooldownMs = 2000;
const timeoutMs = 30_000;
let service: AdminAuthService;
let requests: HttpTestingController;
let navigate: ReturnType<typeof vi.fn>;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
    navigate = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(withInterceptors([adminAuthInterceptor])),
            provideHttpClientTesting(),
            { provide: Router, useValue: { url: '/unauthorized?reason=unavailable&returnUrl=%2Fusers', navigate } },
        ],
    });
    service = TestBed.inject(AdminAuthService);
    requests = TestBed.inject(HttpTestingController);
});
afterEach(() => {
    requests.verify();
    TestBed.resetTestingModule();
    vi.useRealTimers();
    vi.restoreAllMocks();
});

describe('administrative restoration transport', () => {
    it.each([HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden])(
        'lets the recovery page handle restoration rejection %s',
        async status => {
            const restored = service.restoreSessionAsync();
            requests.expectOne(`${base}/session`).flush({}, { status, statusText: 'Rejected' });
            await restored;
            expect(
                navigate,
                'Restoration rejection must not replace the recovery return URL with a recursive unauthorized route',
            ).not.toHaveBeenCalled();
            expect(service.isAuthenticated()).toBe(false);
            expect(service.sessionUnavailable()).toBe(false);
        },
    );

    it('coalesces restoration, honors Retry-After and recovers without a new SSO exchange', async () => {
        let now = Date.now();
        vi.spyOn(Date, 'now').mockImplementation(() => now);
        const first = service.restoreSessionAsync();
        const second = service.restoreSessionAsync(true);
        requests
            .expectOne(`${base}/session`)
            .flush({}, { status: HttpStatusCode.TooManyRequests, statusText: 'Limited', headers: { 'Retry-After': '2' } });
        await first;
        await second;
        expect(service.sessionUnavailable()).toBe(true);
        await service.restoreSessionAsync(true);
        requests.expectNone(`${base}/session`);
        now += cooldownMs;
        const retried = service.restoreSessionAsync(true);
        requests.expectOne(`${base}/session`).flush(response);
        await retried;
        expect(service.sessionUnavailable()).toBe(false);
        expect(service.isAdmin()).toBe(true);
        const known = service.restoreSessionAsync(true);
        const proof = requests.expectOne(`${base}/session`);
        expect(proof.request.headers.get('X-FoodDiary-Session')).toBe(response.sessionId);
        expect(proof.request.headers.get('X-FoodDiary-Client')).toBe('admin');
        proof.flush(response);
        await known;
        requests.expectNone(`${base}/admin-sso/exchange`);
    });

    it('turns a stalled request into a retryable outage', async () => {
        vi.useFakeTimers();
        const restored = service.restoreSessionAsync();
        const stalled = requests.expectOne(`${base}/session`);
        await vi.advanceTimersByTimeAsync(timeoutMs);
        await restored;
        expect(stalled.cancelled).toBe(true);
        expect(service.sessionUnavailable()).toBe(true);
        expect(navigate).not.toHaveBeenCalled();
    });
});
