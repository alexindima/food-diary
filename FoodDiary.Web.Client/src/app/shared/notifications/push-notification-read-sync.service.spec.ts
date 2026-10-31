import { DOCUMENT } from '@angular/common';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { AuthService } from '../../services/auth.service';
import { SessionEventsService } from '../auth/session-events.service';
import { entityId, type NotificationId } from '../models/semantics/entity-id';
import { BrowserNotificationCapabilityService } from './browser-notification-capability.service';
import { NotificationService } from './notification.service';
import { PushNotificationReadSyncService } from './push-notification-read-sync.service';

const firstId = entityId<'notification'>('11111111-1111-1111-1111-111111111111');
const secondId = entityId<'notification'>('22222222-2222-2222-2222-222222222222');
const FLUSH_ATTEMPTS = 12;
const UUID_SUFFIX_LENGTH = 12;
const BATCH_SIZE = 100;
const PUSH_COUNT = 101;
const STATUS_TIMEOUT_MS = 5000;

type DisplayedPush = { data: { notificationId: NotificationId; userId: string }; close: Mock<() => void> };

function push(notificationId = firstId, userId = 'user-a'): DisplayedPush {
    return { data: { notificationId, userId }, close: vi.fn() };
}

let displayed: DisplayedPush[];
let readIds: Set<NotificationId>;
let changes: Subject<void>;
let browser: { getDisplayedNotificationsAsync: ReturnType<typeof vi.fn> };
let notifications: {
    readStateChanged$: Subject<void>;
    getReadNotificationIds: Mock<(ids: readonly NotificationId[]) => Observable<NotificationId[]>>;
};
let authenticated: ReturnType<typeof signal<boolean>>;
let impersonating: ReturnType<typeof signal<boolean>>;
let userId: ReturnType<typeof signal<string>>;
let document: EventTarget & { visibilityState: string; defaultView: EventTarget };

beforeEach(() => {
    displayed = [push(), push(secondId)];
    readIds = new Set([firstId]);
    changes = new Subject<void>();
    browser = {
        getDisplayedNotificationsAsync: vi.fn(
            async () =>
                new Promise(resolve => {
                    resolve(displayed);
                }),
        ),
    };
    notifications = {
        readStateChanged$: changes,
        getReadNotificationIds: vi.fn((ids: readonly NotificationId[]) => of(ids.filter(id => readIds.has(id)))),
    };
    authenticated = signal(true);
    impersonating = signal(false);
    userId = signal('user-a');
    document = Object.assign(new EventTarget(), { visibilityState: 'visible', defaultView: new EventTarget() });
    TestBed.configureTestingModule({
        providers: [
            { provide: DOCUMENT, useValue: document },
            { provide: BrowserNotificationCapabilityService, useValue: browser },
            { provide: NotificationService, useValue: notifications },
            { provide: AuthService, useValue: { isAuthenticated: authenticated, isImpersonating: impersonating, getUserId: userId } },
        ],
    });
});

afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
});

async function startAsync(): Promise<void> {
    TestBed.inject(PushNotificationReadSyncService);
    await settleAsync();
}

async function settleAsync(): Promise<void> {
    TestBed.tick();
    for (let index = 0; index < FLUSH_ATTEMPTS; index++) {
        await waitForAsyncTasksAsync();
    }
}

describe('PushNotificationReadSyncService', () => {
    it('closes only server-confirmed reads on startup, including pushes absent from the feed', async () => {
        displayed.push(push(firstId, 'user-b'), { data: { notificationId: firstId, userId: '' }, close: vi.fn() });
        await startAsync();

        expect(notifications.getReadNotificationIds).toHaveBeenCalledExactlyOnceWith([firstId, secondId]);
        expect(displayed[0].close).toHaveBeenCalledOnce();
        for (const notification of displayed.slice(1)) {
            expect(notification.close).not.toHaveBeenCalled();
        }
    });

    it('rechecks remote reads after realtime or polling hints even if the unread count is unchanged', async () => {
        readIds.clear();
        await startAsync();
        readIds.add(secondId);

        changes.next();
        await settleAsync();

        expect(displayed[0].close).not.toHaveBeenCalled();
        expect(displayed[1].close).toHaveBeenCalledOnce();
    });

    it.each(['visibilitychange', 'focus', 'online'])('reconciles when the browser resumes through %s', async event => {
        readIds.clear();
        await startAsync();
        readIds.add(firstId);
        (event === 'visibilitychange' ? document : document.defaultView).dispatchEvent(new Event(event));
        await settleAsync();
        expect(displayed[0].close).toHaveBeenCalledOnce();
    });

    it('does not make status requests without displayed pushes', async () => {
        displayed = [];
        await startAsync();
        expect(notifications.getReadNotificationIds).not.toHaveBeenCalled();
    });

    it('keeps pushes on API or browser errors and retries after the next hint', async () => {
        notifications.getReadNotificationIds.mockReturnValueOnce(throwError(() => new Error('offline')));
        await startAsync();
        expect(displayed[0].close).not.toHaveBeenCalled();
        browser.getDisplayedNotificationsAsync.mockRejectedValueOnce(new Error('worker unavailable'));
        changes.next();
        await settleAsync();
        expect(displayed[0].close).not.toHaveBeenCalled();
        changes.next();
        await settleAsync();
        expect(displayed[0].close).toHaveBeenCalledOnce();
    });
});

describe('PushNotificationReadSyncService session isolation', () => {
    it.each(['logout', 'switch', 'impersonation', 'invalidate', 'session-end', 'destroy'])(
        'ignores in-flight status after %s',
        async action => {
            const pending = new Subject<NotificationId[]>();
            notifications.getReadNotificationIds.mockReturnValueOnce(pending);
            await startAsync();
            const sessions = TestBed.inject(SessionEventsService);
            if (action === 'logout') {
                authenticated.set(false);
            }
            if (action === 'switch') {
                userId.set('user-b');
            }
            if (action === 'impersonation') {
                impersonating.set(true);
            }
            if (action === 'invalidate') {
                sessions.notifyLocalSessionInvalidating();
            }
            if (action === 'session-end') {
                sessions.notifySessionEnded();
            }
            if (action === 'destroy') {
                TestBed.resetTestingModule();
            }

            pending.next([firstId]);
            pending.complete();
            if (action !== 'destroy') {
                await settleAsync();
            } else {
                await waitForAsyncTasksAsync();
            }

            expect(displayed[0].close).not.toHaveBeenCalled();
        },
    );

    it('does not query while impersonating and resumes after normal authentication', async () => {
        impersonating.set(true);
        await startAsync();
        expect(browser.getDisplayedNotificationsAsync).not.toHaveBeenCalled();
        impersonating.set(false);
        TestBed.inject(SessionEventsService).notifyAuthenticated();
        await settleAsync();
        expect(displayed[0].close).toHaveBeenCalledOnce();
    });
});

describe('PushNotificationReadSyncService concurrent changes and failures', () => {
    it('coalesces concurrent hints and reruns after an in-flight stale status response', async () => {
        const pending = new Subject<NotificationId[]>();
        notifications.getReadNotificationIds.mockReturnValueOnce(pending);
        await startAsync();
        changes.next();
        changes.next();
        expect(notifications.getReadNotificationIds).toHaveBeenCalledOnce();
        pending.next([]);
        pending.complete();
        await settleAsync();
        expect(notifications.getReadNotificationIds).toHaveBeenCalledTimes(2);
        expect(displayed[0].close).toHaveBeenCalledOnce();
    });

    it('deduplicates and batches more than 100 displayed notifications', async () => {
        displayed = Array.from({ length: PUSH_COUNT }, (_, index) =>
            push(entityId<'notification'>(`11111111-1111-1111-1111-${String(index + 1).padStart(UUID_SUFFIX_LENGTH, '0')}`)),
        );
        displayed.push(displayed[0]);
        await startAsync();
        expect(notifications.getReadNotificationIds.mock.calls.map(call => call[0].length)).toEqual([BATCH_SIZE, 1]);
    });

    it('continues after one OS dismissal fails', async () => {
        readIds.add(secondId);
        displayed[0].close.mockImplementation(() => {
            throw new Error('OS failure');
        });
        await startAsync();
        expect(displayed[1].close).toHaveBeenCalledOnce();
    });

    it('times out a stuck request so the next resume can retry', async () => {
        vi.useFakeTimers();
        notifications.getReadNotificationIds.mockReturnValueOnce(new Subject<NotificationId[]>());
        await startAsync();
        await vi.advanceTimersByTimeAsync(STATUS_TIMEOUT_MS);
        document.defaultView.dispatchEvent(new Event('online'));
        await settleAsync();
        expect(displayed[0].close).toHaveBeenCalledOnce();
    });
});
