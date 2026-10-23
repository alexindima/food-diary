import { HTTP_INTERCEPTORS, HttpClient, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { SessionEventsService } from '../shared/auth/session-events.service';
import { AuthInterceptor } from './auth.interceptor';

const path = '/api/v1/notifications/read-all';
let http: HttpClient;
let requests: HttpTestingController;
let sessions: SessionEventsService;
let userId: ReturnType<typeof signal<string>>;
let impersonating: ReturnType<typeof signal<boolean>>;
let refresh: Subject<string | null>;
let refreshToken: ReturnType<typeof vi.fn>;
let completed: ReturnType<typeof vi.fn<(value: unknown) => void>>;
let failed: ReturnType<typeof vi.fn<(error: unknown) => void>>;

beforeEach(() => {
    userId = signal('user-a');
    impersonating = signal(false);
    refresh = new Subject<string | null>();
    refreshToken = vi.fn(() => refresh);
    completed = vi.fn();
    failed = vi.fn();
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(withInterceptorsFromDi()),
            provideHttpClientTesting(),
            { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
            {
                provide: AuthService,
                useValue: { getToken: vi.fn(() => 'old-token'), getUserId: userId, isImpersonating: impersonating, refreshToken },
            },
        ],
    });
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
    sessions = TestBed.inject(SessionEventsService);
});

afterEach(() => {
    requests.verify();
});

describe('AuthInterceptor request owner before refresh', () => {
    it('does not refresh or replay an old PUT after the signed-in account changes', () => {
        http.put(path, {}).subscribe({ next: completed, error: failed });
        const original = requests.expectOne(path);
        userId.set('user-b');
        sessions.notifyAuthenticated();
        original.flush(null, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

        expect(refreshToken).not.toHaveBeenCalled();
        requests.expectNone(path);
        expect(completed).not.toHaveBeenCalled();
        expect(failed).toHaveBeenCalledWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));
    });

    it('invalidates the old PUT when the same account signs in again', () => {
        http.put(path, {}).subscribe({ error: failed });
        const original = requests.expectOne(path);
        sessions.notifySessionEnded();
        sessions.notifyAuthenticated();
        original.flush(null, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

        expect(refreshToken).not.toHaveBeenCalled();
        requests.expectNone(path);
        expect(failed).toHaveBeenCalledOnce();
    });

    it('does not reuse a normal request after impersonation starts', () => {
        http.put(path, {}).subscribe({ error: failed });
        const original = requests.expectOne(path);
        impersonating.set(true);
        original.flush(null, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

        expect(refreshToken).not.toHaveBeenCalled();
        requests.expectNone(path);
        expect(failed).toHaveBeenCalledOnce();
    });
});

describe('AuthInterceptor request owner after refresh', () => {
    it('does not replay the PUT when the account changes while refresh is pending', () => {
        http.put(path, {}).subscribe({ next: completed, error: failed });
        requests.expectOne(path).flush(null, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        expect(refreshToken).toHaveBeenCalledOnce();
        userId.set('user-b');
        sessions.notifyAuthenticated();
        refresh.next('new-account-token');
        refresh.complete();

        requests.expectNone(path);
        expect(completed).not.toHaveBeenCalled();
        expect(failed).toHaveBeenCalledWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));
    });

    it('preserves same-account refresh and the original PUT payload', () => {
        const body = { marker: 'same-account-payload' };
        http.put(path, body).subscribe({ next: completed, error: failed });
        const original = requests.expectOne(path);
        expect(original.request.headers.get('Authorization')).toBe('Bearer old-token');
        original.flush(null, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        refresh.next('refreshed-same-account-token');
        refresh.complete();

        const retry = requests.expectOne(path);
        expect(retry.request.method).toBe('PUT');
        expect(retry.request.body).toEqual(body);
        expect(retry.request.headers.get('Authorization')).toBe('Bearer refreshed-same-account-token');
        retry.flush(null);
        expect(completed).toHaveBeenCalledOnce();
        expect(failed).not.toHaveBeenCalled();
    });
});
