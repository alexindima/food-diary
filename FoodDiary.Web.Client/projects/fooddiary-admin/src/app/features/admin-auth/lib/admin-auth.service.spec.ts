import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AdminAuthService } from './admin-auth.service';

const base = environment.apiUrls.auth;
const response = { sessionId: 'admin-session', roles: ['User', 'Admin'], user: { id: 'admin-user' } };
let service: AdminAuthService;
let requests: HttpTestingController;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminAuthService);
    requests = TestBed.inject(HttpTestingController);
});
afterEach(() => {
    requests.verify();
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
});

describe('administrative browser sessions', () => {
    it('gets its administrative identity from the SSO response without persisting a token', async () => {
        const exchanged = service.exchangeSsoCodeAsync('one-use-code');
        const request = requests.expectOne(`${base  }/admin-sso/exchange`);
        expect(request.request.withCredentials).toBe(true);
        request.flush(response);
        expect(await exchanged).toBe(true);
        expect(service.isAdmin()).toBe(true);
        expect(service.getSessionId()).toBe('admin-session');
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBeNull();
    });

    it('restores the separate administrative cookie after reloading', async () => {
        const restored = service.restoreSessionAsync();
        requests.expectOne(`${base  }/session`).flush(response);
        await restored;
        expect(service.isAuthenticated()).toBe(true);
        expect(service.isAdmin()).toBe(true);
    });

    it('does not treat a legacy bearer credential in storage as a signed-in session', () => {
        localStorage.setItem('authToken', 'old-token');
        expect(service.isAuthenticated()).toBe(false);
        expect(service.isAdmin()).toBe(false);
    });

    it('exchanges a fragment code and removes the code from the visible URL', async () => {
        window.history.replaceState({}, '', '/users?page=2#code=one-use-code');
        const applied = service.applySsoFromQueryAsync();
        requests.expectOne(`${base  }/admin-sso/exchange`).flush(response);
        await applied;
        expect(window.location.hash).toBe('');
        expect(window.location.search).toBe('?page=2');
        expect(service.isAdmin()).toBe(true);
    });

    it('cleans a return URL only after a successful one-use exchange', async () => {
        const applied = service.tryApplySsoFromReturnUrlAsync('/users?page=3#code=code-for-return');
        requests.expectOne(`${base  }/admin-sso/exchange`).flush(response);
        expect(await applied).toBe('/users?page=3');
    });

    it('does not infer administrator permissions from an incomplete response', async () => {
        const applied = service.exchangeSsoCodeAsync('invalid-response');
        requests.expectOne(`${base  }/admin-sso/exchange`).flush({ sessionId: 'admin-session' });
        expect(await applied).toBe(false);
        expect(service.isAuthenticated()).toBe(false);
    });

    it('preserves the rate-limit hint and clears it on a subsequent successful exchange', async () => {
        const limited = service.exchangeSsoCodeAsync('limited');
        requests
            .expectOne(`${base  }/admin-sso/exchange`)
            .flush({}, { status: HttpStatusCode.TooManyRequests, statusText: 'Too Many Requests' });
        expect(await limited).toBe(false);
        expect(service.ssoRateLimited()).toBe(true);
        const next = service.exchangeSsoCodeAsync('new-code');
        requests.expectOne(`${base  }/admin-sso/exchange`).flush(response);
        expect(await next).toBe(true);
        expect(service.ssoRateLimited()).toBe(false);
    });
});
