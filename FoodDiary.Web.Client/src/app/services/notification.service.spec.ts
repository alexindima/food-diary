import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../constants/global-loading-context.tokens';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';

const UNREAD_COUNT_INITIAL = 4;
const UNREAD_COUNT_CONCURRENT = 7;
const UNREAD_COUNT_LOADED = 3;
const UNREAD_COUNT_FORCED = 5;

const baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');

let service: NotificationService;
let httpMock: HttpTestingController;
let authService: { isAuthenticated: ReturnType<typeof vi.fn> };

beforeEach(() => {
    authService = {
        isAuthenticated: vi.fn(() => true),
    };

    TestBed.configureTestingModule({
        providers: [NotificationService, provideHttpClient(), provideHttpClientTesting(), { provide: AuthService, useValue: authService }],
    });

    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
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
});

describe('NotificationService read state', () => {
    it('should mark notification as read and update local state', () => {
        service.notifications.set([
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
        service.unreadCount.set(1);

        service.markAsRead('n1').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/n1/read`);
        expect(req.request.method).toBe('PUT');
        req.flush(null);

        expect(service.unreadCount()).toBe(0);
        expect(service.notifications()[0].isRead).toBe(true);
    });

    it('should mark all notifications as read and clear unread count', () => {
        service.notifications.set([
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
            {
                id: 'n2',
                type: 'info',
                title: 'Title 2',
                body: null,
                targetUrl: null,
                referenceId: null,
                isRead: false,
                createdAtUtc: '2026-01-01T00:00:00Z',
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

describe('NotificationService settings endpoints', () => {
    it('should call preferences and subscription endpoints with expected payloads', () => {
        service.getNotificationPreferences().subscribe();
        httpMock.expectOne(`${baseUrl}/preferences`).flush({
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: false,
            socialPushNotificationsEnabled: true,
        });

        service.updateNotificationPreferences({ pushNotificationsEnabled: false }).subscribe();
        const updateReq = httpMock.expectOne(`${baseUrl}/preferences`);
        expect(updateReq.request.method).toBe('PUT');
        expect(updateReq.request.body).toEqual({ pushNotificationsEnabled: false });
        updateReq.flush({
            pushNotificationsEnabled: false,
            fastingPushNotificationsEnabled: true,
            socialPushNotificationsEnabled: true,
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
