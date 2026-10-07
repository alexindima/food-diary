import {
    HttpClient,
    HttpContext,
    HttpContextToken,
    HttpHeaders,
    type HttpInterceptorFn,
    HttpParams,
    provideHttpClient,
    withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { AdminAuthSdk } from './generated/api/admin-auth.service';
import { AdminUsersSdk } from './generated/api/admin-users.service';
import { createSdkConnection, sdkRequestOptions } from './sdk-connection';

const bypassLoading = new HttpContextToken(() => false);
const auth: HttpInterceptorFn = (request, next) => next(request.clone({ setHeaders: { Authorization: 'Bearer test-only' } }));
let http: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(withInterceptors([auth])), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
});
afterEach(() => {
    http.verify();
});

describe('Admin generated client transport', () => {
    it('retains repeated filters, false, zero and decoded Unicode through the existing interceptor chain', () => {
        const sdk = createSdkConnection(AdminUsersSdk, '/api/v1/admin/users', TestBed.inject(HttpClient));
        const params = new HttpParams()
            .append('role', 'Admin & User')
            .append('role', 'Observer')
            .set('includeDeleted', false)
            .set('custom', 'значение');
        sdk.client
            .getAdminUsers({ version: sdk.version, page: 1, limit: 0 }, 'body', false, sdkRequestOptions(undefined, params))
            .subscribe();
        const request = http.expectOne(value => value.url === '/api/v1/admin/users');
        expect(request.request.params.getAll('role')).toEqual(['Admin & User', 'Observer']);
        expect(request.request.params.get('includeDeleted')).toBe('false');
        expect(request.request.params.get('limit')).toBe('0');
        expect(request.request.params.get('custom')).toBe('значение');
        expect(request.request.headers.get('Authorization')).toBe('Bearer test-only');
        expect(request.request.withCredentials).toBe(false);
        request.flush({});
    });

    it('preserves adapter headers and HttpContext flags', () => {
        const sdk = createSdkConnection(AdminUsersSdk, '/api/v1/admin/users', TestBed.inject(HttpClient));
        const options = sdkRequestOptions(new HttpHeaders({ 'X-Trace': 'trace' }), undefined, new HttpContext().set(bypassLoading, true));
        sdk.client.getAdminUsersById({ version: sdk.version, id: 'id' }, 'body', false, options).subscribe();
        const request = http.expectOne('/api/v1/admin/users/id');
        expect(request.request.headers.get('X-Trace')).toBe('trace');
        expect(request.request.context.get(bypassLoading)).toBe(true);
        request.flush({});
    });

    it('enables cookies only for the SSO exchange connection', () => {
        const sdk = createSdkConnection(AdminAuthSdk, '/api/v1/auth', TestBed.inject(HttpClient), true);
        sdk.client.postAuthAdminSsoExchange({ version: sdk.version, adminSsoExchangeHttpRequest: { code: 'one-use-code' } }).subscribe();
        const request = http.expectOne('/api/v1/auth/admin-sso/exchange');
        expect(request.request.withCredentials).toBe(true);
        expect(request.request.body).toEqual({ code: 'one-use-code' });
        request.flush({ accessToken: 'token' });
    });

    it('cancels HTTP when the consuming subscription is cancelled', () => {
        const sdk = createSdkConnection(AdminUsersSdk, '/api/v1/admin/users', TestBed.inject(HttpClient));
        const subscription = sdk.client.getAdminUsers({ version: sdk.version }).subscribe();
        const request = http.expectOne('/api/v1/admin/users');
        subscription.unsubscribe();
        expect(request.cancelled).toBe(true);
    });
});
