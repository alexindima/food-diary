import { HttpClient, HttpContext, HttpHeaders, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import { NotificationsSdk } from './generated/api/notifications.service';
import { ProductsSdk } from './generated/api/products.service';
import { createSdkConnection, sdkRequestOptions } from './sdk-connection';

describe('SDK connection for background notification reads', () => {
    let http: HttpClient;
    let requests: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        http = TestBed.inject(HttpClient);
        requests = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        requests.verify();
    });

    it.each([
        ['/gateway/api/notifications', 'API URL must include /api/v{version}.'],
        ['/gateway/api/v/notifications', 'API URL must include a version.'],
    ])('rejects the misconfigured endpoint %s before it can send a request', (baseUrl, message) => {
        expect(() => createSdkConnection(NotificationsSdk, baseUrl, http)).toThrow(message);
        requests.expectNone(() => true);
    });

    it('keeps session cookies and the silent loading context when only fetch priority is configured', async () => {
        const baseUrl = '/gateway/api/v1/notifications';
        const sdk = createSdkConnection(NotificationsSdk, baseUrl, http, { priority: 'low' });
        const context = new HttpContext().set(SKIP_GLOBAL_LOADING, true);
        const result = firstValueFrom(
            sdk.client.getNotificationsUnreadCount({ version: sdk.version }, 'body', false, sdkRequestOptions(undefined, context)),
        );
        const request = requests.expectOne({ method: 'GET', url: `${baseUrl}/unread-count` });

        expect(request.request.withCredentials).toBe(true);
        expect(request.request.priority).toBe('low');
        expect(request.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        request.flush({ count: 2 });

        await expect(result).resolves.toEqual({ count: 2 });
    });

    it('preserves SDK default headers, intentional empty values and the original search text', () => {
        const baseUrl = '/gateway/api/v1/products';
        const sdk = createSdkConnection(ProductsSdk, baseUrl, http);
        const headers = new HttpHeaders({ aCcEpT: [], 'X-Trace': ['first', 'second'], 'X-Present-Empty': '' });
        const search = 'Чай + мёд & 50%26';
        const context = new HttpContext().set(SKIP_GLOBAL_LOADING, true);

        sdk.client
            .getProductsOverview(
                { version: sdk.version, search, limit: 0, includePublic: false },
                'body',
                false,
                sdkRequestOptions(headers, context),
            )
            .subscribe();

        const request = requests.expectOne(value => value.url === `${baseUrl}/overview`);
        expect(request.request.headers.get('Accept')).toBe('application/json');
        expect(request.request.headers.getAll('x-trace')).toEqual(['first', 'second']);
        expect(request.request.headers.getAll('X-Present-Empty')).toEqual(['']);
        expect(request.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        expect(request.request.params.get('search')).toBe(search);
        expect(request.request.params.get('includePublic')).toBe('false');
        expect(request.request.params.get('limit')).toBe('0');
        request.flush({});
    });
});
