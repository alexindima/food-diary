import { HttpClient, HttpStatusCode, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { adminAuthInterceptor } from './admin-auth.interceptor';
import { AdminAuthService } from './admin-auth.service';

const root = environment.apiUrls.auth.replace(/\/auth$/, '');
let http: HttpClient;
let requests: HttpTestingController;
let router: { url: string; navigate: ReturnType<typeof vi.fn> };
let auth: { getSessionId: ReturnType<typeof vi.fn>; invalidateSession: ReturnType<typeof vi.fn> };

function configureTransport(platform = 'browser'): void {
    auth = { getSessionId: vi.fn(() => 'admin-session'), invalidateSession: vi.fn() };
    router = { url: '/users?page=2', navigate: vi.fn().mockResolvedValue(true) };
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(withInterceptors([adminAuthInterceptor])),
            provideHttpClientTesting(),
            { provide: PLATFORM_ID, useValue: platform },
            { provide: AdminAuthService, useValue: auth },
            { provide: Router, useValue: router },
        ],
    });
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
}

beforeEach(() => {
    configureTransport();
});
afterEach(() => {
    requests.verify();
});

describe('administrative cookie transport', () => {
    it('sends administrative selectors and the CSRF header without a bearer credential', () => {
        http.get(`${root}/admin/users`).subscribe();
        const request = requests.expectOne(`${root}/admin/users`);
        expect(request.request.headers.get('X-FoodDiary-Client')).toBe('admin');
        expect(request.request.headers.get('X-FoodDiary-Session')).toBe('admin-session');
        expect(request.request.headers.get('X-Requested-With')).toBe('XMLHttpRequest');
        expect(request.request.headers.has('Authorization')).toBe(false);
        expect(request.request.withCredentials).toBe(true);
        request.flush({});
    });

    it('does not attach credentials to another service', () => {
        http.get('https://external.example/resource').subscribe();
        const request = requests.expectOne('https://external.example/resource');
        expect(request.request.withCredentials).toBe(false);
        expect(request.request.headers.keys()).toEqual([]);
        request.flush({});
    });

    it('invalidates local administrative state on 401', () => {
        http.get(`${root}/admin/users`).subscribe({ error: () => {} });
        requests.expectOne(`${root}/admin/users`).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        expect(auth.invalidateSession).toHaveBeenCalledOnce();
        expect(router.navigate).toHaveBeenCalledWith(['/sign-in'], {
            queryParams: { returnUrl: '/users?page=2' },
        });
    });

    it.each([HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden])('ignores a late %s belonging to a replaced session', status => {
        http.get(`${root}/admin/users`).subscribe({ error: () => {} });
        const pending = requests.expectOne(`${root}/admin/users`);
        auth.getSessionId.mockReturnValue('fresh-admin-session');
        pending.flush({}, { status, statusText: 'Denied' });
        expect(auth.invalidateSession, 'A stale response must not invalidate the new administrative session').not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('retains feature-specific handling for AI consent failures', () => {
        http.post(`${root}/admin/ai-prompts/test`, {}).subscribe({ error: () => {} });
        requests
            .expectOne(`${root}/admin/ai-prompts/test`)
            .flush({ error: 'Ai.ConsentRequired' }, { status: HttpStatusCode.Forbidden, statusText: 'Forbidden' });
        expect(router.navigate).not.toHaveBeenCalled();
    });
});

describe('administrative transport recovery', () => {
    it('opens permission feedback on a forbidden feature request without invalidating the session', () => {
        const errors = vi.fn();
        const usersUrl = `${root}/admin/users`;
        http.get(usersUrl).subscribe({ error: errors });
        requests.expectOne(usersUrl).flush({}, { status: HttpStatusCode.Forbidden, statusText: 'Forbidden' });

        expect(errors).toHaveBeenCalledOnce();
        expect(auth.invalidateSession).not.toHaveBeenCalled();
        expect(router.navigate).toHaveBeenCalledExactlyOnceWith(['/unauthorized'], {
            queryParams: { reason: 'forbidden', returnUrl: router.url },
        });
    });

    it('continues to offer sign-in when legacy browser storage is blocked', () => {
        const unavailableStorage = vi.spyOn(window, 'localStorage', 'get').mockImplementation(() => {
            throw new DOMException('Storage is disabled for this origin', 'SecurityError');
        });
        try {
            sessionStorage.setItem('authToken', 'legacy-test-value');
            http.get(`${root}/admin/users`).subscribe({ error: vi.fn() });
            requests.expectOne(`${root}/admin/users`).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

            expect(sessionStorage.getItem('authToken')).toBeNull();
            expect(auth.invalidateSession).toHaveBeenCalledOnce();
            expect(router.navigate).toHaveBeenCalledExactlyOnceWith(['/sign-in'], { queryParams: { returnUrl: router.url } });
        } finally {
            unavailableStorage.mockRestore();
            sessionStorage.removeItem('authToken');
        }
    });

    it('passes a rejected cookie request through in the server runtime without reading browser storage', () => {
        TestBed.resetTestingModule();
        configureTransport('server');
        const localAccess = vi.spyOn(window, 'localStorage', 'get');
        const sessionAccess = vi.spyOn(window, 'sessionStorage', 'get');
        try {
            const errors = vi.fn();
            http.get(`${root}/admin/users`).subscribe({ error: errors });
            requests.expectOne(`${root}/admin/users`).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

            expect(errors).toHaveBeenCalledOnce();
            expect(localAccess).not.toHaveBeenCalled();
            expect(sessionAccess).not.toHaveBeenCalled();
        } finally {
            localAccess.mockRestore();
            sessionAccess.mockRestore();
        }
    });
});
