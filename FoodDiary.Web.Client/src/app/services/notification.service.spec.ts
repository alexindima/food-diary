import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NotificationService } from './notification.service';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

describe('NotificationService', () => {
    let service: NotificationService;
    let httpMock: HttpTestingController;
    let authService: { isAuthenticated: ReturnType<typeof vi.fn> };

    const baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');

    beforeEach(() => {
        authService = {
            isAuthenticated: vi.fn(() => true),
        };

        TestBed.configureTestingModule({
            providers: [
                NotificationService,
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: AuthService, useValue: authService },
            ],
        });

        service = TestBed.inject(NotificationService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should load notifications only once via ensureNotificationsLoaded', () => {
        service.ensureNotificationsLoaded();
        service.ensureNotificationsLoaded();

        const req = httpMock.expectOne(baseUrl);
        expect(req.request.method).toBe('GET');
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
        req.flush({ count: 4 });

        expect(service.unreadCount()).toBe(4);
    });

    it('should reset unread count when unauthenticated', () => {
        authService.isAuthenticated.mockReturnValue(false);

        service.fetchUnreadCount();

        httpMock.expectNone(`${baseUrl}/unread-count`);
        expect(service.unreadCount()).toBe(0);
    });

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
