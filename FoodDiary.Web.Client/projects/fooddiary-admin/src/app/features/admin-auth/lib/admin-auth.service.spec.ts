import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AdminAuthService } from './admin-auth.service';

const BASE_URL = environment.apiUrls.auth;

let service: AdminAuthService;
let httpMock: HttpTestingController;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');

    TestBed.configureTestingModule({
        providers: [AdminAuthService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AdminAuthService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
});

describe('AdminAuthService token state', () => {
    it('should detect admin role from token payload', () => {
        localStorage.setItem('authToken', createToken({ role: 'Admin' }));
        service.refreshTokenState();

        expect(service.isAuthenticated()).toBe(true);
        expect(service.isAdmin()).toBe(true);
    });

    it('should capture auth token from query and clear token params', () => {
        const replaceStateSpy = vi.spyOn(window.history, 'replaceState');
        window.history.replaceState({}, '', '/?authToken=query-token&foo=1');

        service.refreshTokenState();

        expect(service.getToken()).toBe('query-token');
        expect(replaceStateSpy).toHaveBeenCalledWith({}, '', '/?foo=1');
    });
});

describe('AdminAuthService SSO exchange', () => {
    it('should exchange sso code from query and clear code from url', async () => {
        const replaceStateSpy = vi.spyOn(window.history, 'replaceState');
        window.history.replaceState({}, '', '/admin?code=sso-code&foo=1');

        const promise = service.applySsoFromQueryAsync();

        const req = httpMock.expectOne(`${BASE_URL}/admin-sso/exchange`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ code: 'sso-code' });
        req.flush({
            accessToken: createToken({ role: 'Admin' }),
            refreshToken: 'refresh-token',
        });

        await promise;

        expect(localStorage.getItem('authToken')).toBeTruthy();
        expect(localStorage.getItem('refreshToken')).toBe('refresh-token');
        expect(sessionStorage.getItem('adminSsoCode')).toBe('sso-code');
        expect(replaceStateSpy).toHaveBeenCalledWith({}, '', '/admin?foo=1');
    });

    it('should extract code from return url and return cleaned path after successful exchange', async () => {
        const resultPromise = service.tryApplySsoFromReturnUrlAsync(encodeURIComponent('/users?page=2&code=return-code'));

        const req = httpMock.expectOne(`${BASE_URL}/admin-sso/exchange`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ code: 'return-code' });
        req.flush({
            accessToken: createToken({ role: 'Admin' }),
            refreshToken: 'refresh-token',
        });

        await expect(resultPromise).resolves.toBe('/users?page=2');
        expect(sessionStorage.getItem('adminSsoCode')).toBe('return-code');
    });
});

describe('AdminAuthService admin upgrade', () => {
    it('should start admin sso and exchange returned code when upgrading to admin', async () => {
        localStorage.setItem('authToken', createToken({ role: 'User' }));
        service.refreshTokenState();

        const promise = service.tryUpgradeToAdminAsync();

        const startReq = httpMock.expectOne(`${BASE_URL}/admin-sso/start`);
        expect(startReq.request.method).toBe('POST');
        startReq.flush({ code: 'upgrade-code' });

        await Promise.resolve();

        const exchangeReq = httpMock.expectOne(`${BASE_URL}/admin-sso/exchange`);
        expect(exchangeReq.request.method).toBe('POST');
        expect(exchangeReq.request.body).toEqual({ code: 'upgrade-code' });
        exchangeReq.flush({
            accessToken: createToken({ role: 'Admin' }),
            refreshToken: 'refresh-token',
        });

        await expect(promise).resolves.toBe(true);
        expect(service.isAdmin()).toBe(true);
    });
});

function createToken(payload: Record<string, unknown>): string {
    const header = encodeSegment({ alg: 'none', typ: 'JWT' });
    const body = encodeSegment(payload);
    return `${header}.${body}.signature`;
}

function encodeSegment(value: Record<string, unknown>): string {
    return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/u, '');
}
