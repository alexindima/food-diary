import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import type { NotificationHttpResponse } from '../../../shared/api/sdk/generated/model/notification-http-response';
import { NotificationService } from '../../../shared/notifications/notification.service';
import { NotificationsDialogComponent } from './notifications-dialog';

const FEED_URL = environment.apiUrls.auth.replace('/auth', '/notifications');
const FEED_REQUEST = { method: 'GET', url: FEED_URL };
const SAVED_ITEM = {
    id: 'existing-notification',
    type: 'info',
    title: 'A previously loaded notification',
    body: null,
    targetUrl: null,
    referenceId: null,
    isRead: false,
    createdAtUtc: '2026-01-01T00:00:00Z',
} satisfies NotificationHttpResponse;

it.each([false, true])('allows in-place recovery from a failed notification request with cached rows=%s', async cached => {
    TestBed.configureTestingModule({
        imports: [NotificationsDialogComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([]),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
            {
                provide: AuthService,
                useValue: { isAuthenticated: vi.fn(() => true), getUserId: vi.fn(() => 'user'), isImpersonating: vi.fn(() => false) },
            },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        http.verify();
    });
    const fixture = TestBed.createComponent(NotificationsDialogComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    if (cached) {
        http.expectOne(FEED_REQUEST).flush([SAVED_ITEM]);
        fixture.detectChanges();
        expect(host.textContent).toContain(SAVED_ITEM.title);
        TestBed.inject(NotificationService).refreshNotifications();
        fixture.detectChanges();
    }

    http.expectOne(FEED_REQUEST).flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('fd-ui-inline-alert[role="alert"]')?.textContent ?? '').toContain('ERRORS.LOAD_FAILED_MESSAGE');
    });
    expect(host.querySelector('.notifications-dialog__state--empty')).toBeNull();
    expect(host.textContent.includes(SAVED_ITEM.title)).toBe(cached);

    const retry = host.querySelector<HTMLButtonElement>('fd-ui-inline-alert button');
    if (retry === null) {
        throw new Error('Expected the notification retry action');
    }
    expect(retry.textContent).toContain('ERRORS.RETRY');
    retry.focus();
    retry.click();
    fixture.detectChanges();
    expect(document.activeElement).toBe(host.querySelector('.notifications-dialog__body'));
    http.expectOne(FEED_REQUEST).flush([]);
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('.notifications-dialog__state--empty')?.textContent ?? '').toContain('NOTIFICATIONS.EMPTY_TITLE');
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
    });
    expect(document.activeElement).toBe(host.querySelector('.notifications-dialog__body'));
});
