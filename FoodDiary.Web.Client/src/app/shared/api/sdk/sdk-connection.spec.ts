import { HttpClient, HttpContext, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import { NotificationsSdk } from './generated/api/notifications.service';
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
});
