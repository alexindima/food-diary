import {
    HTTP_INTERCEPTORS,
    HttpClient,
    HttpContext,
    HttpStatusCode,
    provideHttpClient,
    withInterceptorsFromDi,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../environments/environment';
import { SKIP_AUTH } from '../constants/http-context.tokens';
import { AuthService } from '../services/auth.service';
import { BrowserSessionStateService } from '../shared/auth/browser-session-state.service';
import { AuthInterceptor } from './auth.interceptor';

const root = environment.apiUrls.auth.replace(/\/auth$/, '');
let http: HttpClient;
let requests: HttpTestingController;
let auth: {
    getToken: ReturnType<typeof vi.fn>;
    refreshSession: ReturnType<typeof vi.fn>;
    getUserId: ReturnType<typeof vi.fn>;
    isImpersonating: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    auth = {
        getToken: vi.fn(() => null),
        refreshSession: vi.fn(() => of(true)),
        getUserId: vi.fn(() => 'user-a'),
        isImpersonating: vi.fn(() => false),
    };
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(withInterceptorsFromDi()),
            provideHttpClientTesting(),
            { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
            { provide: AuthService, useValue: auth },
        ],
    });
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
});
afterEach(() => {
    requests.verify();
});

describe('browser session request protection', () => {
    it('uses browser credentials and the CSRF header without adding a bearer token', () => {
        http.post(`${root}/meals`, {}).subscribe();
        const request = requests.expectOne(`${root}/meals`);
        expect(request.request.withCredentials).toBe(true);
        expect(request.request.headers.get('X-Requested-With')).toBe('XMLHttpRequest');
        expect(request.request.headers.get('X-FoodDiary-Client')).toBe('web');
        expect(request.request.headers.has('Authorization')).toBe(false);
        request.flush({});
    });

    it('binds a mutation and logout to the expected session identity', () => {
        TestBed.inject(BrowserSessionStateService).sessionId.set('session-a');
        for (const path of ['/meals', '/auth/logout']) {
            http.post(root + path, {}).subscribe();
            const request = requests.expectOne(root + path);
            expect(request.request.headers.get('X-FoodDiary-Session')).toBe('session-a');
            request.flush({});
        }
    });

    it('does not pin login or session restoration to a previous identity', () => {
        TestBed.inject(BrowserSessionStateService).sessionId.set('old-session');
        http.post(`${root}/auth/session`, {}).subscribe();
        const request = requests.expectOne(`${root}/auth/session`);
        expect(request.request.headers.has('X-FoodDiary-Session')).toBe(false);
        request.flush({});
    });

    it('adds an explicit impersonation context without exposing its secret', () => {
        TestBed.inject(BrowserSessionStateService).contextId.set('context-a');
        http.get(`${root}/users/info`).subscribe();
        const request = requests.expectOne(`${root}/users/info`);
        expect(request.request.headers.get('X-FoodDiary-Context')).toBe('context-a');
        expect(request.request.headers.has('Authorization')).toBe(false);
        request.flush({});
    });

    it('never sends authentication metadata or credentials to external storage', () => {
        auth.getToken.mockReturnValue('telegram-access');
        http.put('https://storage.example/upload', {}, { context: new HttpContext().set(SKIP_AUTH, true) }).subscribe();
        const request = requests.expectOne('https://storage.example/upload');
        expect(request.request.withCredentials).toBe(false);
        expect(request.request.headers.keys()).toEqual([]);
        request.flush({});
    });

    it('retains the explicit token transport used by Telegram', () => {
        auth.getToken.mockReturnValue('telegram-access');
        http.get(`${root}/meals`).subscribe();
        const request = requests.expectOne(`${root}/meals`);
        expect(request.request.headers.get('Authorization')).toBe('Bearer telegram-access');
        request.flush({});
    });
});

describe('browser session revalidation', () => {
    it('updates role metadata after a policy denial without replaying the operation', () => {
        const failed = vi.fn();
        http.post(`${root}/products`, {}).subscribe({ error: failed });
        requests.expectOne(`${root}/products`).flush({ error: 'Authentication.Forbidden' }, { status: 403, statusText: 'Forbidden' });
        expect(auth.refreshSession).toHaveBeenCalledOnce();
        requests.expectNone(`${root}/products`);
        expect(failed).toHaveBeenCalledOnce();
    });

    it('revalidates a session on 401 and preserves the original mutation', () => {
        const completed = vi.fn();
        http.put(`${root}/meals/one`, { value: 3 }).subscribe(completed);
        requests.expectOne(`${root}/meals/one`).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        const retried = requests.expectOne(`${root}/meals/one`);
        expect(auth.refreshSession).toHaveBeenCalledOnce();
        expect(retried.request.body).toEqual({ value: 3 });
        expect(retried.request.headers.has('Authorization')).toBe(false);
        retried.flush({});
        expect(completed).toHaveBeenCalledOnce();
    });

    it('does not retry when session revalidation fails', () => {
        auth.refreshSession.mockReturnValue(of(false));
        const failed = vi.fn();
        http.put(`${root}/meals/one`, {}).subscribe({ error: failed });
        requests.expectOne(`${root}/meals/one`).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        requests.expectNone(`${root}/meals/one`);
        expect(failed).toHaveBeenCalledOnce();
    });

    it('does not reinterpret an incorrect current password as an expired session', () => {
        const failed = vi.fn();
        http.post(`${root}/users/password`, {}).subscribe({ error: failed });
        requests
            .expectOne(`${root}/users/password`)
            .flush({ error: 'User.InvalidPassword' }, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        expect(auth.refreshSession).not.toHaveBeenCalled();
        expect(failed).toHaveBeenCalledOnce();
    });

    it('does not recursively retry login or session restoration', () => {
        for (const path of ['/auth/login', '/auth/session']) {
            http.post(root + path, {}).subscribe({ error: () => {} });
            requests.expectOne(root + path).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        }
        expect(auth.refreshSession).not.toHaveBeenCalled();
    });

    it.each(['/auth/telegram/mini-app/begin', '/auth/telegram/mini-app/begin-link'])(
        'does not refresh a session when fresh Telegram proof is rejected at %s',
        path => {
            auth.refreshSession.mockReturnValue(of(false));
            const failed = vi.fn();
            http.post(root + path, { initData: 'invalid-proof' }).subscribe({ error: failed });
            requests
                .expectOne(root + path)
                .flush({ error: 'Telegram.InvalidProof' }, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
            expect(auth.refreshSession).not.toHaveBeenCalled();
            expect(failed).toHaveBeenCalledOnce();
        },
    );
});
