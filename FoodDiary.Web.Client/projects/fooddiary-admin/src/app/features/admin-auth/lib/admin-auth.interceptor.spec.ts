import { HttpClient, HttpStatusCode, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
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

beforeEach(() => {
    auth = { getSessionId: vi.fn(() => 'admin-session'), invalidateSession: vi.fn() };
    router = { url: '/users?page=2', navigate: vi.fn().mockResolvedValue(true) };
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(withInterceptors([adminAuthInterceptor])),
            provideHttpClientTesting(),
            { provide: AdminAuthService, useValue: auth },
            { provide: Router, useValue: router },
        ],
    });
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
});
afterEach(() => { requests.verify(); });

describe('administrative cookie transport', () => {
    it('sends administrative selectors and the CSRF header without a bearer credential', () => {
        http.get(`${root  }/admin/users`).subscribe();
        const request = requests.expectOne(`${root  }/admin/users`);
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
        http.get(`${root  }/admin/users`).subscribe({ error: () => {} });
        requests.expectOne(`${root  }/admin/users`).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        expect(auth.invalidateSession).toHaveBeenCalledOnce();
        expect(router.navigate).toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'unauthenticated', returnUrl: '/users?page=2' },
        });
    });

    it('retains feature-specific handling for AI consent failures', () => {
        http.post(`${root  }/admin/ai-prompts/test`, {}).subscribe({ error: () => {} });
        requests
            .expectOne(`${root  }/admin/ai-prompts/test`)
            .flush({ error: 'Ai.ConsentRequired' }, { status: HttpStatusCode.Forbidden, statusText: 'Forbidden' });
        expect(router.navigate).not.toHaveBeenCalled();
    });
});
