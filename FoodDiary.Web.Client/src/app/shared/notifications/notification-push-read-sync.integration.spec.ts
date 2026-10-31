import { DOCUMENT } from '@angular/common';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { AuthService } from '../../services/auth.service';
import { entityId } from '../models/semantics/entity-id';
import { BrowserNotificationCapabilityService } from './browser-notification-capability.service';
import { NotificationService } from './notification.service';
import { PushNotificationReadSyncService } from './push-notification-read-sync.service';

const baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');
const id = entityId<'notification'>('11111111-1111-1111-1111-111111111111');
const FLUSH_ATTEMPTS = 12;
let http: HttpTestingController;
let notifications: NotificationService;
let close: ReturnType<typeof vi.fn>;

beforeEach(async () => {
    close = vi.fn();
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: DOCUMENT, useValue: Object.assign(new EventTarget(), { defaultView: null }) },
            {
                provide: AuthService,
                useValue: { isAuthenticated: signal(true), isImpersonating: signal(false), getUserId: signal('user-a') },
            },
            {
                provide: BrowserNotificationCapabilityService,
                useValue: {
                    getDisplayedNotificationsAsync: vi.fn().mockResolvedValue([{ data: { notificationId: id, userId: 'user-a' }, close }]),
                },
            },
        ],
    });
    http = TestBed.inject(HttpTestingController);
    notifications = TestBed.inject(NotificationService);
    TestBed.inject(PushNotificationReadSyncService);
    await settleAsync();
    http.expectOne(`${baseUrl}/read-status`).flush({ readIds: [] });
    await settleAsync();
});

afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
});

async function settleAsync(): Promise<void> {
    TestBed.tick();
    for (let index = 0; index < FLUSH_ATTEMPTS; index++) {
        await waitForAsyncTasksAsync();
    }
}

describe('Notification read to displayed push integration', () => {
    it('closes after successful read persistence and leaves a failed read visible', async () => {
        notifications.markAsRead(id).subscribe({ error: () => {} });
        http.expectOne(`${baseUrl}/${id}/read`).flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'unavailable' });
        await settleAsync();
        http.expectNone(`${baseUrl}/read-status`);
        expect(close).not.toHaveBeenCalled();

        notifications.markAsRead(id).subscribe();
        expect(close).not.toHaveBeenCalled();
        http.expectOne(`${baseUrl}/${id}/read`).flush(null);
        await settleAsync();
        http.expectOne(`${baseUrl}/read-status`).flush({ readIds: [id] });
        await settleAsync();
        expect(close).toHaveBeenCalledOnce();
    });

    it('closes after mark-all-read and after a remote reconciliation hint', async () => {
        notifications.markAllRead().subscribe();
        http.expectOne(`${baseUrl}/read-all`).flush(null);
        await settleAsync();
        http.expectOne(`${baseUrl}/read-status`).flush({ readIds: [id] });
        await settleAsync();
        expect(close).toHaveBeenCalledOnce();

        notifications.updateCount(1);
        await settleAsync();
        http.expectOne(`${baseUrl}/read-status`).flush({ readIds: [id] });
        await settleAsync();
        expect(close).toHaveBeenCalledTimes(2);
    });
});
