import { HTTP_INTERCEPTORS, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import { SKIP_AUTH } from '../../constants/http-context.tokens';
import { AuthInterceptor } from '../../interceptor/auth.interceptor';
import { AuthService } from '../../services/auth.service';
import { SessionEventsService } from '../auth/session-events.service';
import type { NotificationItem } from '../models/notification.data';
import { decodeNotificationKind } from '../models/notification-kind';
import { utcInstant } from '../models/semantics/date-value';
import { entityId } from '../models/semantics/entity-id';
import { NotificationService } from './notification.service';

const UNREAD_COUNT_INITIAL = 4;
const UNREAD_COUNT_CONCURRENT = 7;
const UNREAD_COUNT_LOADED = 3;
const UNREAD_COUNT_FORCED = 5;
const UNREAD_COUNT_ALREADY_READ = 3;

const baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');

let service: NotificationService;
let sessionEvents: SessionEventsService;
let httpMock: HttpTestingController;
let authService: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    getUserId: ReturnType<typeof vi.fn>;
    isImpersonating: ReturnType<typeof vi.fn>;
    getToken: ReturnType<typeof vi.fn>;
    refreshSession: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    authService = {
        isAuthenticated: vi.fn(() => true),
        getUserId: vi.fn(() => 'user-a'),
        isImpersonating: vi.fn(() => false),
        getToken: vi.fn(() => null),
        refreshSession: vi.fn(() => of(true)),
    };

    TestBed.configureTestingModule({
        providers: [NotificationService, provideHttpClient(), provideHttpClientTesting(), { provide: AuthService, useValue: authService }],
    });

    service = TestBed.inject(NotificationService);
    sessionEvents = TestBed.inject(SessionEventsService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('NotificationService push read synchronization', () => {
    const id = entityId<'notification'>('11111111-1111-1111-1111-111111111111');
    const expectedRefreshes = 3;

    it('queries bounded read status through the SDK and normalizes IDs', () => {
        const next = vi.fn();
        service.getReadNotificationIds([id]).subscribe(next);
        const request = httpMock.expectOne(`${baseUrl}/read-status`);
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toEqual({ notificationIds: [id] });
        expect(request.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        request.flush({ readIds: [id.toUpperCase()] });
        expect(next).toHaveBeenCalledExactlyOnceWith([id]);
    });

    it('rejects incomplete status responses instead of inferring reads', () => {
        const error = vi.fn();
        service.getReadNotificationIds([id]).subscribe({ error });
        httpMock.expectOne(`${baseUrl}/read-status`).flush({});
        expect(error).toHaveBeenCalledOnce();
    });

    it('signals only successfully persisted local reads', () => {
        const changed = vi.fn();
        service.readStateChanged$.subscribe(changed);
        service.markAsRead(id).subscribe();
        expect(changed).not.toHaveBeenCalled();
        httpMock.expectOne(`${baseUrl}/${id}/read`).flush(null);
        expect(changed).toHaveBeenCalledOnce();
        service.markAllRead().subscribe({ error: () => {} });
        httpMock.expectOne(`${baseUrl}/read-all`).flush(null, { status: 500, statusText: 'offline' });
        expect(changed).toHaveBeenCalledOnce();
        service.markAllRead().subscribe();
        httpMock.expectOne(`${baseUrl}/read-all`).flush(null);
        expect(changed).toHaveBeenCalledTimes(2);
    });

    it('signals reconciliation even when the remote unread count has not changed', () => {
        const changed = vi.fn();
        service.readStateChanged$.subscribe(changed);
        service.updateCount(1);
        service.updateCount(1);
        service.fetchUnreadCount({ force: true });
        httpMock.expectOne(`${baseUrl}/unread-count`).flush({ count: 1 });
        expect(changed).toHaveBeenCalledTimes(expectedRefreshes);
    });
});

describe('NotificationService loading', () => {
    it('should load notifications only once via ensureNotificationsLoaded', () => {
        service.ensureNotificationsLoaded();
        service.ensureNotificationsLoaded();

        const req = httpMock.expectOne(baseUrl);
        expect(req.request.method).toBe('GET');
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        req.flush([
            {
                id: 'n1',
                type: 'info',
                title: 'Title',
                body: null,
                targetUrl: null,
                referenceId: null,
                isRead: false,
                createdAtUtc: '2026-01-01T00:00:00Z',
            },
        ]);

        expect(service.notificationsLoaded()).toBe(true);
        expect(service.notifications()).toHaveLength(1);
    });

    it('should update unread count when fetchUnreadCount succeeds', () => {
        service.fetchUnreadCount();

        const req = httpMock.expectOne(`${baseUrl}/unread-count`);
        expect(req.request.method).toBe('GET');
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        req.flush({ count: UNREAD_COUNT_INITIAL });

        expect(service.unreadCount()).toBe(UNREAD_COUNT_INITIAL);
    });

    it('should coalesce concurrent unread count requests', () => {
        service.fetchUnreadCount();
        service.fetchUnreadCount();
        service.fetchUnreadCount();

        const requests = httpMock.match(`${baseUrl}/unread-count`);
        expect(requests).toHaveLength(1);

        requests[0].flush({ count: UNREAD_COUNT_CONCURRENT });

        expect(service.unreadCount()).toBe(UNREAD_COUNT_CONCURRENT);
    });

    it('should not refetch unread count after it has been loaded', () => {
        service.fetchUnreadCount();

        const req = httpMock.expectOne(`${baseUrl}/unread-count`);
        req.flush({ count: UNREAD_COUNT_LOADED });

        service.fetchUnreadCount();

        httpMock.expectNone(`${baseUrl}/unread-count`);
        expect(service.unreadCount()).toBe(UNREAD_COUNT_LOADED);
    });

    it('should refetch unread count when forced', () => {
        service.fetchUnreadCount();

        const firstReq = httpMock.expectOne(`${baseUrl}/unread-count`);
        firstReq.flush({ count: UNREAD_COUNT_LOADED });

        service.fetchUnreadCount({ force: true });

        const secondReq = httpMock.expectOne(`${baseUrl}/unread-count`);
        secondReq.flush({ count: UNREAD_COUNT_FORCED });

        expect(service.unreadCount()).toBe(UNREAD_COUNT_FORCED);
    });

    it('should reset unread count when unauthenticated', () => {
        authService.isAuthenticated.mockReturnValue(false);

        service.fetchUnreadCount();

        httpMock.expectNone(`${baseUrl}/unread-count`);
        expect(service.unreadCount()).toBe(0);
    });

    it('should allow retry after notifications loading fails', () => {
        service.ensureNotificationsLoaded();

        const failedReq = httpMock.expectOne(baseUrl);
        failedReq.flush(null, { status: 500, statusText: 'Server Error' });

        expect(service.notificationsLoaded()).toBe(false);
        expect(service.notificationsLoading()).toBe(false);

        service.ensureNotificationsLoaded();

        const retryReq = httpMock.expectOne(baseUrl);
        expect(retryReq.request.method).toBe('GET');
        retryReq.flush([]);

        expect(service.notificationsLoaded()).toBe(true);
    });
});

describe('NotificationService acknowledgement refresh', () => {
    it('replaces pre-click count reads and ignores their late responses', () => {
        service.fetchUnreadCount();
        const stale = httpMock.expectOne(`${baseUrl}/unread-count`);
        service.fetchUnreadCount({ force: true, supersedePending: true });
        const fresh = httpMock.match(`${baseUrl}/unread-count`);
        expect(fresh, 'acknowledgement must replace pending count reads').toHaveLength(1);
        fresh[0].flush({ count: 0 });
        stale.flush({ count: UNREAD_COUNT_INITIAL });
        expect(service.unreadCount()).toBe(0);
    });

    it('retains the latest pending count request when a superseded read finishes', () => {
        service.fetchUnreadCount();
        const stale = httpMock.expectOne(`${baseUrl}/unread-count`);
        service.fetchUnreadCount({ force: true, supersedePending: true });
        const fresh = httpMock.match(`${baseUrl}/unread-count`);
        expect(fresh, 'acknowledgement must replace pending count reads').toHaveLength(1);
        stale.flush({ count: UNREAD_COUNT_INITIAL });
        service.fetchUnreadCount();
        httpMock.expectNone(`${baseUrl}/unread-count`);
        fresh[0].flush({ count: 0 });
        expect(service.unreadCount()).toBe(0);
    });

    it('discards a pre-click feed and reloads it after the pending read finishes', () => {
        service.ensureNotificationsLoaded();
        const stale = httpMock.expectOne(baseUrl);
        service.notifyNotificationsChanged({ supersedePending: true });
        stale.flush([notificationForSession('Clicked notification')]);
        expect(service.notifications(), 'a pre-click feed must not restore an unread notification').toEqual([]);
        const fresh = httpMock.expectOne(baseUrl);
        fresh.flush([{ ...notificationForSession('Clicked notification'), isRead: true }]);
        expect(service.notifications()[0].isRead).toBe(true);
    });
});

describe('NotificationService session ownership', () => {
    it('reads cached state without HTTP and clears the previous owner synchronously', () => {
        service.notifications.set([notificationForSession('Private title')]);
        service.notificationsLoaded.set(true);
        service.updateCount(UNREAD_COUNT_INITIAL);
        expect(service.readState()).toMatchObject({ unreadCount: UNREAD_COUNT_INITIAL, loaded: true });
        authService.getUserId.mockReturnValue('user-b');
        expect(service.readState()).toMatchObject({ unreadCount: null, items: [], loaded: false, requested: false });
        httpMock.expectNone(baseUrl);
        httpMock.expectNone(`${baseUrl}/unread-count`);
    });

    it.each([
        { boundary: 'authentication', result: 'success' },
        { boundary: 'authentication', result: 'error' },
        { boundary: 'session end', result: 'success' },
        { boundary: 'session end', result: 'error' },
        { boundary: 'account change', result: 'success' },
        { boundary: 'account change', result: 'error' },
    ] as const)('ignores old read $result and finalization after $boundary', ({ boundary, result }) => {
        service.fetchUnreadCount();
        service.ensureNotificationsLoaded();
        const oldCount = httpMock.expectOne(`${baseUrl}/unread-count`);
        const oldList = httpMock.expectOne(baseUrl);

        if (boundary === 'authentication') {
            sessionEvents.notifyAuthenticated();
        } else if (boundary === 'session end') {
            sessionEvents.notifySessionEnded();
        } else {
            authService.getUserId.mockReturnValue('user-b');
        }
        service.fetchUnreadCount();
        service.ensureNotificationsLoaded();
        const currentCount = httpMock.expectOne(`${baseUrl}/unread-count`);
        const currentList = httpMock.expectOne(baseUrl);

        if (result === 'success') {
            oldCount.flush({ count: UNREAD_COUNT_INITIAL });
            oldList.flush([notificationForSession('Old session')]);
        } else {
            oldCount.flush(null, { status: 500, statusText: 'Server Error' });
            oldList.flush(null, { status: 500, statusText: 'Server Error' });
        }

        expect(service.unreadCount()).toBe(0);
        expect(service.notifications()).toEqual([]);
        expect(service.notificationsLoaded()).toBe(false);
        expect(service.notificationsLoading()).toBe(true);
        service.fetchUnreadCount({ force: true });
        service.ensureNotificationsLoaded();
        httpMock.expectNone(`${baseUrl}/unread-count`);
        httpMock.expectNone(baseUrl);

        currentCount.flush({ count: UNREAD_COUNT_FORCED });
        currentList.flush([notificationForSession('Current session')]);

        expect(service.unreadCount()).toBe(UNREAD_COUNT_FORCED);
        expect(service.notifications()[0].title).toBe('Current session');
        expect(service.notificationsLoaded()).toBe(true);
        expect(service.notificationsLoading()).toBe(false);
    });

    it('keeps current data when the previous account succeeds after current reads finish', () => {
        service.fetchUnreadCount();
        service.ensureNotificationsLoaded();
        const oldCount = httpMock.expectOne(`${baseUrl}/unread-count`);
        const oldList = httpMock.expectOne(baseUrl);

        sessionEvents.notifySessionEnded();
        sessionEvents.notifyAuthenticated();
        service.fetchUnreadCount();
        service.ensureNotificationsLoaded();
        httpMock.expectOne(`${baseUrl}/unread-count`).flush({ count: UNREAD_COUNT_FORCED });
        httpMock.expectOne(baseUrl).flush([notificationForSession('Relogged-in session')]);

        oldCount.flush({ count: UNREAD_COUNT_INITIAL });
        oldList.flush([notificationForSession('Old session')]);

        expect(service.unreadCount()).toBe(UNREAD_COUNT_FORCED);
        expect(service.notifications()[0].title).toBe('Relogged-in session');
        service.fetchUnreadCount();
        httpMock.expectNone(`${baseUrl}/unread-count`);
    });
});

describe('NotificationService mutation session ownership', () => {
    it.each(['one', 'all'] as const)('ignores late mark-%s-read effects after same-account relogin', action => {
        const request = action === 'one' ? service.markAsRead(entityId<'notification'>('n1')) : service.markAllRead();
        request.subscribe();
        const pending = httpMock.expectOne(`${baseUrl}/${action === 'one' ? 'n1/read' : 'read-all'}`);

        sessionEvents.notifySessionEnded();
        sessionEvents.notifyAuthenticated();
        service.notifications.set([notificationForSession('Relogged-in session')]);
        service.updateCount(UNREAD_COUNT_FORCED);
        pending.flush(null);

        expect(service.unreadCount()).toBe(UNREAD_COUNT_FORCED);
        expect(service.notifications()[0].isRead).toBe(false);
    });

    it('clears all loaded state and ignores in-flight results after logout', () => {
        service.notifications.set([notificationForSession('Old session')]);
        service.notificationsLoaded.set(true);
        service.updateCount(UNREAD_COUNT_INITIAL);
        service.notifyNotificationsChanged();
        service.fetchUnreadCount({ force: true });
        const pendingList = httpMock.expectOne(baseUrl);
        const pendingCount = httpMock.expectOne(`${baseUrl}/unread-count`);

        sessionEvents.notifySessionEnded();
        authService.isAuthenticated.mockReturnValue(false);
        pendingList.flush([notificationForSession('Late old session')]);
        pendingCount.flush({ count: UNREAD_COUNT_CONCURRENT });

        expect(service.notifications()).toEqual([]);
        expect(service.unreadCount()).toBe(0);
        expect(service.notificationsLoading()).toBe(false);
        expect(service.notificationsLoaded()).toBe(false);
        expect(service.notificationsChangedVersion()).toBe(0);
        service.ensureNotificationsLoaded();
        service.fetchUnreadCount();
        httpMock.expectNone(baseUrl);
        httpMock.expectNone(`${baseUrl}/unread-count`);
    });
});

function notificationForSession(title: string): NotificationItem {
    return {
        id: entityId<'notification'>('n1'),
        type: decodeNotificationKind('info'),
        title,
        body: null,
        targetUrl: null,
        referenceId: null,
        isRead: false,
        createdAtUtc: utcInstant('2026-01-01T00:00:00Z'),
    };
}

describe('NotificationService read state', () => {
    it('should mark notification as read and update local state', () => {
        service.notifications.set([
            {
                id: entityId<'notification'>('n1'),
                type: decodeNotificationKind('info'),
                title: 'Title',
                body: null,
                targetUrl: null,
                referenceId: null,
                isRead: false,
                createdAtUtc: utcInstant('2026-01-01T00:00:00Z'),
            },
        ]);
        service.unreadCount.set(1);

        service.markAsRead(entityId<'notification'>('n1')).subscribe();

        const req = httpMock.expectOne(`${baseUrl}/n1/read`);
        expect(req.request.method).toBe('PUT');
        req.flush(null);

        expect(service.unreadCount()).toBe(0);
        expect(service.notifications()[0].isRead).toBe(true);
    });

    it('should not decrement unread count when notification is already read locally', () => {
        service.notifications.set([
            {
                id: entityId<'notification'>('n1'),
                type: decodeNotificationKind('info'),
                title: 'Title',
                body: null,
                targetUrl: null,
                referenceId: null,
                isRead: true,
                createdAtUtc: utcInstant('2026-01-01T00:00:00Z'),
            },
        ]);
        service.unreadCount.set(UNREAD_COUNT_ALREADY_READ);

        service.markAsRead(entityId<'notification'>('n1')).subscribe();

        const req = httpMock.expectOne(`${baseUrl}/n1/read`);
        expect(req.request.method).toBe('PUT');
        req.flush(null);

        expect(service.unreadCount()).toBe(UNREAD_COUNT_ALREADY_READ);
        expect(service.notifications()[0].isRead).toBe(true);
    });

    it('should mark all notifications as read and clear unread count', () => {
        service.notifications.set([
            {
                id: entityId<'notification'>('n1'),
                type: decodeNotificationKind('info'),
                title: 'Title',
                body: null,
                targetUrl: null,
                referenceId: null,
                isRead: false,
                createdAtUtc: utcInstant('2026-01-01T00:00:00Z'),
            },
            {
                id: entityId<'notification'>('n2'),
                type: decodeNotificationKind('info'),
                title: 'Title 2',
                body: null,
                targetUrl: null,
                referenceId: null,
                isRead: false,
                createdAtUtc: utcInstant('2026-01-01T00:00:00Z'),
            },
        ]);
        service.unreadCount.set(2);

        service.markAllRead().subscribe();

        const req = httpMock.expectOne(`${baseUrl}/read-all`);
        expect(req.request.method).toBe('PUT');
        req.flush(null);

        expect(service.unreadCount()).toBe(0);
        expect(service.notifications().every(x => x.isRead)).toBe(true);
    });
});

describe('NotificationService push request session ownership', () => {
    it('uses the captured session token for logout cleanup after the account changes', () => {
        const endpoint = 'https://push.example.com/subscriptions/account-a';
        const pending = service.removeWebPushSubscription(endpoint, 'token-a');

        authService.getUserId.mockReturnValue('user-b');
        sessionEvents.notifyAuthenticated();
        pending.subscribe();

        const request = httpMock.expectOne(`${baseUrl}/push/subscription`);
        expect(request.request.method).toBe('DELETE');
        expect(request.request.headers.get('X-FoodDiary-Session')).toBe('token-a');
        expect(request.request.headers.has('Authorization')).toBe(false);
        expect(request.request.context.get(SKIP_AUTH)).toBe(true);
        request.flush(null);
    });

    it.each(['upsert', 'remove'] as const)('preserves the normal authentication path for push %s without a captured token', action => {
        const endpoint = 'https://push.example.com/subscriptions/current';
        const request$ =
            action === 'upsert'
                ? service.upsertWebPushSubscription({
                      endpoint,
                      expirationTime: null,
                      keys: { p256dh: 'p256', auth: 'auth' },
                      locale: 'en',
                      userAgent: 'Chrome',
                  })
                : service.removeWebPushSubscription(endpoint);
        request$.subscribe();

        const request = httpMock.expectOne(`${baseUrl}/push/subscription`);
        expect(request.request.headers.has('Authorization')).toBe(false);
        expect(request.request.context.get(SKIP_AUTH)).toBe(false);
        request.flush(null);
    });
});

describe('NotificationService push token refresh', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                NotificationService,
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
                { provide: AuthService, useValue: authService },
            ],
        });
        service = TestBed.inject(NotificationService);
        sessionEvents = TestBed.inject(SessionEventsService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    it.each(['upsert', 'remove'] as const)('refreshes an expired token and retries a normal push %s request', action => {
        const endpoint = 'https://push.example.com/subscriptions/current';
        const request$ =
            action === 'upsert'
                ? service.upsertWebPushSubscription({
                      endpoint,
                      expirationTime: null,
                      keys: { p256dh: 'p256', auth: 'auth' },
                      locale: 'en',
                      userAgent: 'Chrome',
                  })
                : service.removeWebPushSubscription(endpoint);
        const completed = vi.fn();
        request$.subscribe({ complete: completed });

        const initial = httpMock.expectOne(`${baseUrl}/push/subscription`);
        expect(initial.request.headers.has('Authorization')).toBe(false);
        expect(initial.request.context.get(SKIP_AUTH)).toBe(false);
        initial.flush(null, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

        expect(authService.refreshSession).toHaveBeenCalledTimes(1);
        const retried = httpMock.expectOne(`${baseUrl}/push/subscription`);
        expect(retried.request.method).toBe(action === 'upsert' ? 'PUT' : 'DELETE');
        expect(retried.request.headers.has('Authorization')).toBe(false);
        expect(retried.request.context.get(SKIP_AUTH)).toBe(false);
        retried.flush(null);
        expect(completed).toHaveBeenCalledTimes(1);
    });
});

describe('NotificationService settings endpoints', () => {
    it('should call preferences and subscription endpoints with expected payloads', () => {
        service.getNotificationPreferences().subscribe();
        httpMock.expectOne(`${baseUrl}/preferences`).flush({
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: false,
            socialPushNotificationsEnabled: true,
            fastingCheckInReminderHours: 12,
            fastingCheckInFollowUpReminderHours: 20,
        });

        service.updateNotificationPreferences({ pushNotificationsEnabled: false }).subscribe();
        const updateReq = httpMock.expectOne(`${baseUrl}/preferences`);
        expect(updateReq.request.method).toBe('PUT');
        expect(updateReq.request.body).toEqual({ pushNotificationsEnabled: false });
        updateReq.flush({
            pushNotificationsEnabled: false,
            fastingPushNotificationsEnabled: true,
            socialPushNotificationsEnabled: true,
            fastingCheckInReminderHours: 12,
            fastingCheckInFollowUpReminderHours: 20,
        });

        service.getWebPushSubscriptions().subscribe();
        httpMock.expectOne(`${baseUrl}/push/subscriptions`).flush([]);
    });

    it('should call push config upsert and remove endpoints', () => {
        service.getWebPushConfiguration().subscribe();
        httpMock.expectOne(`${baseUrl}/push/config`).flush({ enabled: true, publicKey: 'key' });

        service
            .upsertWebPushSubscription({
                endpoint: 'https://push.example.com/subscriptions/1',
                expirationTime: null,
                keys: { p256dh: 'p256', auth: 'auth' },
                locale: 'en',
                userAgent: 'Chrome',
            })
            .subscribe();
        const upsertReq = httpMock.expectOne(`${baseUrl}/push/subscription`);
        expect(upsertReq.request.method).toBe('PUT');
        upsertReq.flush(null);

        service.removeWebPushSubscription('https://push.example.com/subscriptions/1').subscribe();
        const removeReq = httpMock.expectOne(`${baseUrl}/push/subscription`);
        expect(removeReq.request.method).toBe('DELETE');
        expect(removeReq.request.body).toEqual({ endpoint: 'https://push.example.com/subscriptions/1' });
        removeReq.flush(null);
    });

    it('should increment notificationsChangedVersion when notifications change is reported', () => {
        expect(service.notificationsChangedVersion()).toBe(0);

        service.notifyNotificationsChanged();

        expect(service.notificationsChangedVersion()).toBe(1);
        const req = httpMock.expectOne(baseUrl);
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });
});
