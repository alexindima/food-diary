import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { SessionEventsService } from '../auth/session-events.service';
import { NotificationService } from './notification.service';

const FIRST_COUNT = 4;
const SECOND_COUNT = 7;
const baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');
let service: NotificationService;
let http: HttpTestingController;
let sessions: SessionEventsService;
let authenticated: ReturnType<typeof signal<boolean>>;
let userId: ReturnType<typeof signal<string>>;
let impersonating: ReturnType<typeof signal<boolean>>;

beforeEach(() => {
    authenticated = signal(true);
    userId = signal('user-a');
    impersonating = signal(false);
    TestBed.configureTestingModule({
        providers: [
            NotificationService,
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: { isAuthenticated: authenticated, getUserId: userId, isImpersonating: impersonating } },
        ],
    });
    service = TestBed.inject(NotificationService);
    http = TestBed.inject(HttpTestingController);
    sessions = TestBed.inject(SessionEventsService);
    TestBed.tick();
});

afterEach(() => {
    http.verify();
});

describe('NotificationService session isolation', () => {
    it('ignores pending feed and unread responses after logout', () => {
        service.fetchUnreadCount();
        service.ensureNotificationsLoaded();
        const count = http.expectOne(`${baseUrl}/unread-count`);
        const feed = http.expectOne(baseUrl);

        sessions.notifySessionEnded();
        authenticated.set(false);
        TestBed.tick();
        count.flush({ count: FIRST_COUNT });
        feed.flush([notification('old')]);

        expect(service.unreadCount()).toBe(0);
        expect(service.notifications()).toEqual([]);
        expect(service.notificationsLoaded()).toBe(false);
        expect(service.notificationsLoading()).toBe(false);
        expect(service.notificationsRequested()).toBe(false);
    });

    it.each(['success', 'failure'])('keeps new-owner requests independent of late old-owner %s', outcome => {
        service.fetchUnreadCount();
        service.refreshNotifications();
        const oldCount = http.expectOne(`${baseUrl}/unread-count`);
        const oldFeed = http.expectOne(baseUrl);

        userId.set('user-b');
        service.fetchUnreadCount();
        service.ensureNotificationsLoaded();
        const currentCount = http.expectOne(`${baseUrl}/unread-count`);
        const currentFeed = http.expectOne(baseUrl);
        if (outcome === 'success') {
            oldCount.flush({ count: FIRST_COUNT });
            oldFeed.flush([notification('old')]);
        } else {
            oldCount.flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'unavailable' });
            oldFeed.flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'unavailable' });
        }
        expect(service.notificationsLoading()).toBe(true);
        expect(service.notifications()).toEqual([]);
        expect(service.notificationsRequested()).toBe(true);
        service.fetchUnreadCount();
        service.refreshNotifications();
        http.expectNone(`${baseUrl}/unread-count`);
        http.expectNone(baseUrl);

        currentCount.flush({ count: SECOND_COUNT });
        currentFeed.flush([notification('current')]);
        expect(service.unreadCount()).toBe(SECOND_COUNT);
        expect(service.notifications().map(item => item.title)).toEqual(['current']);
        expect(service.notificationsLoading()).toBe(false);
    });
});

describe('NotificationService session generations', () => {
    it('invalidates pending results even when the same user signs in again before the effect runs', () => {
        service.fetchUnreadCount();
        const previous = http.expectOne(`${baseUrl}/unread-count`);
        sessions.notifySessionEnded();
        sessions.notifyAuthenticated();
        service.fetchUnreadCount();
        const current = http.expectOne(`${baseUrl}/unread-count`);

        previous.flush({ count: FIRST_COUNT });
        service.fetchUnreadCount();
        http.expectNone(`${baseUrl}/unread-count`);
        expect(service.unreadCount()).toBe(0);
        current.flush({ count: SECOND_COUNT });
        expect(service.unreadCount()).toBe(SECOND_COUNT);
    });

    it('resets the feed when impersonation starts and prevents its late response after impersonation ends', () => {
        service.ensureNotificationsLoaded();
        http.expectOne(baseUrl).flush([notification('normal')]);
        impersonating.set(true);
        TestBed.tick();
        expect(service.notificationsRequested()).toBe(false);
        service.ensureNotificationsLoaded();
        const impersonated = http.expectOne(baseUrl);
        expect(service.notifications()).toEqual([]);

        impersonating.set(false);
        TestBed.tick();
        service.ensureNotificationsLoaded();
        const current = http.expectOne(baseUrl);
        impersonated.flush([notification('impersonated')]);
        expect(service.notificationsLoading()).toBe(true);
        current.flush([notification('normal again')]);
        expect(service.notifications().map(item => item.title)).toEqual(['normal again']);
    });

    it('does not apply an old account mark-all completion to a new account', () => {
        service.markAllRead().subscribe();
        const previous = http.expectOne(`${baseUrl}/read-all`);
        userId.set('user-b');
        service.fetchUnreadCount();
        http.expectOne(`${baseUrl}/unread-count`).flush({ count: SECOND_COUNT });
        previous.flush(null);
        expect(service.unreadCount()).toBe(SECOND_COUNT);
    });
});

describe('NotificationService background refresh', () => {
    it('keeps the last good count and feed on failure and coalesces refresh requests', () => {
        service.fetchUnreadCount();
        http.expectOne(`${baseUrl}/unread-count`).flush({ count: FIRST_COUNT });
        service.ensureNotificationsLoaded();
        http.expectOne(baseUrl).flush([notification('retained')]);

        service.fetchUnreadCount({ force: true });
        service.refreshNotifications();
        service.fetchUnreadCount({ force: true });
        service.refreshNotifications();
        http.expectOne(`${baseUrl}/unread-count`).flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'unavailable' });
        http.expectOne(baseUrl).flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'unavailable' });

        expect(service.unreadCount()).toBe(FIRST_COUNT);
        expect(service.notifications().map(item => item.title)).toEqual(['retained']);
        expect(service.notificationsLoaded()).toBe(true);
        expect(service.notificationsLoading()).toBe(false);
        service.fetchUnreadCount({ force: true });
        service.refreshNotifications();
        http.expectOne(`${baseUrl}/unread-count`).flush({ count: SECOND_COUNT });
        http.expectOne(baseUrl).flush([notification('recovered')]);
        expect(service.unreadCount()).toBe(SECOND_COUNT);
        expect(service.notifications().map(item => item.title)).toEqual(['recovered']);
    });
});

function notification(title: string): { id: string; type: string; title: string; isRead: boolean; createdAtUtc: string } {
    return { id: title, type: 'info', title, isRead: false, createdAtUtc: '2026-10-22T10:00:00Z' };
}
