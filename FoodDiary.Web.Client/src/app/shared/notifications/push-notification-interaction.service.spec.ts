import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DefaultUrlSerializer, NavigationEnd, Router, type UrlTree } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { AuthService } from '../../services/auth.service';
import { SessionEventsService } from '../auth/session-events.service';
import { NotificationService } from './notification.service';
import { PushNotificationInteractionService } from './push-notification-interaction.service';

const firstId = '11111111-1111-1111-1111-111111111111';
const secondId = '22222222-2222-2222-2222-222222222222';
let owner = signal<string | null>('user-a');
let impersonating = signal(false);
let service: PushNotificationInteractionService;
let sessions: SessionEventsService;
let events: Subject<NavigationEnd>;
let notifications: {
    markAsRead: ReturnType<typeof vi.fn>;
    fetchUnreadCount: ReturnType<typeof vi.fn>;
    notifyNotificationsChanged: ReturnType<typeof vi.fn>;
};

function configure(url = '/'): void {
    events = new Subject<NavigationEnd>();
    TestBed.configureTestingModule({
        providers: [
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: (): boolean => owner() !== null,
                    isImpersonating: impersonating,
                    getUserId: owner,
                    getSessionKey: (): string | null => (owner() === null ? null : `session-${owner()}`),
                },
            },
            { provide: NotificationService, useValue: notifications },
            {
                provide: Router,
                useValue: {
                    events,
                    url,
                    parseUrl: (value: string): UrlTree => new DefaultUrlSerializer().parse(value),
                    navigateByUrl: vi.fn().mockResolvedValue(true),
                },
            },
        ],
    });
    service = TestBed.inject(PushNotificationInteractionService);
    sessions = TestBed.inject(SessionEventsService);
    TestBed.tick();
}

beforeEach(() => {
    localStorage.clear();
    owner = signal<string | null>('user-a');
    impersonating = signal(false);
    notifications = {
        markAsRead: vi.fn(() => of(void 0)),
        fetchUnreadCount: vi.fn(),
        notifyNotificationsChanged: vi.fn(),
    };
});

describe('Push notification read receipts', () => {
    it('captures an initial closed-app URL and preserves its destination and action', async () => {
        configure(`/fasting?intent=check-in&notificationId=${firstId}&notificationUserId=user-a#details`);
        await vi.waitFor(() => {
            expect(notifications.markAsRead).toHaveBeenCalledWith(firstId);
        });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams['intent']).toBe('check-in');
        expect(router.parseUrl(router.url).fragment).toBe('details');
    });

    it('waits for the intended account when a click arrives before authentication', async () => {
        owner.set(null);
        configure();
        expect(service.recordClick({ data: { notificationId: firstId, userId: 'user-a' } })).toBe(true);
        owner.set('user-b');
        sessions.notifyAuthenticated();
        TestBed.tick();
        await waitForAsyncTasksAsync();
        expect(notifications.markAsRead).not.toHaveBeenCalled();
        owner.set('user-a');
        sessions.notifyAuthenticated();
        TestBed.tick();
        await vi.waitFor(() => {
            expect(notifications.markAsRead).toHaveBeenCalledWith(firstId);
        });
    });

    it('recovers a persisted click after the application restarts', async () => {
        owner.set(null);
        configure();
        service.recordClick({ data: { notificationId: firstId, userId: 'user-a' } });
        TestBed.resetTestingModule();
        owner.set('user-a');
        configure();
        await vi.waitFor(() => {
            expect(notifications.markAsRead).toHaveBeenCalledWith(firstId);
        });
    });
});

describe('Push notification read retries', () => {
    it('retries offline failures on reconnect and updates the bell only after acknowledgement', async () => {
        notifications.markAsRead.mockReturnValue(throwError(() => ({ status: 0 })));
        configure();
        service.recordClick({ data: { notificationId: firstId, userId: 'user-a' } });
        await vi.waitFor(() => {
            expect(notifications.markAsRead).toHaveBeenCalledOnce();
        });
        expect(notifications.fetchUnreadCount).not.toHaveBeenCalled();
        expect(localStorage.getItem('webPushPendingReads:user-a')).toContain(firstId);
        notifications.markAsRead.mockReturnValue(of(void 0));
        window.dispatchEvent(new Event('online'));
        await vi.waitFor(() => {
            expect(notifications.fetchUnreadCount).toHaveBeenCalledWith({ force: true, supersedePending: true });
        });
        expect(localStorage.getItem('webPushPendingReads:user-a')).toBeNull();
    });

    it('does not publish a late acknowledgement into a replacement account', async () => {
        const pending = new Subject<void>();
        notifications.markAsRead.mockReturnValue(pending);
        configure();
        service.recordClick({ data: { notificationId: firstId, userId: 'user-a' } });
        await vi.waitFor(() => {
            expect(notifications.markAsRead).toHaveBeenCalledOnce();
        });
        owner.set('user-b');
        sessions.notifyAuthenticated();
        pending.next();
        await waitForAsyncTasksAsync();
        expect(notifications.fetchUnreadCount).not.toHaveBeenCalled();
        expect(localStorage.getItem('webPushPendingReads:user-a')).toContain(firstId);
    });

    it('deduplicates the push event and navigation and processes a second click during an acknowledgement', async () => {
        const pending = new Subject<void>();
        notifications.markAsRead.mockReturnValueOnce(pending);
        configure();
        service.recordClick({ data: { notificationId: firstId, userId: 'user-a' } });
        const url = `/fasting?notificationId=${firstId}&notificationUserId=user-a`;
        events.next(new NavigationEnd(1, url, url));
        service.recordClick({ data: { notificationId: secondId, userId: 'user-a' } });
        await vi.waitFor(() => {
            expect(notifications.markAsRead).toHaveBeenCalledWith(firstId);
        });
        pending.next();
        await vi.waitFor(() => {
            expect(notifications.markAsRead).toHaveBeenCalledWith(secondId);
        });
        expect(notifications.markAsRead.mock.calls.filter(([id]) => id === firstId)).toHaveLength(1);
    });

    it('uses the legacy notification tag when its owner is signed in', async () => {
        configure();
        service.recordClick({ tag: firstId.replaceAll('-', '') });
        await vi.waitFor(() => {
            expect(notifications.markAsRead).toHaveBeenCalledWith(firstId);
        });
    });
});

describe('Push notification invalid and expired receipts', () => {
    it('drops expired notifications while retaining retryable server errors', async () => {
        notifications.markAsRead.mockReturnValueOnce(throwError(() => ({ status: 404 })));
        configure();
        service.recordClick({ data: { notificationId: firstId, userId: 'user-a' } });
        await vi.waitFor(() => {
            expect(notifications.notifyNotificationsChanged).toHaveBeenCalledOnce();
        });
        expect(localStorage.getItem('webPushPendingReads:user-a')).toBeNull();
    });

    it('does not mark notifications read while impersonating or for malformed identifiers', async () => {
        impersonating.set(true);
        configure();
        expect(service.recordClick({ data: { notificationId: 'invalid', userId: 'user-a' } })).toBe(false);
        service.recordClick({ data: { notificationId: firstId, userId: 'user-a' } });
        await waitForAsyncTasksAsync();
        expect(notifications.markAsRead).not.toHaveBeenCalled();
    });
});
