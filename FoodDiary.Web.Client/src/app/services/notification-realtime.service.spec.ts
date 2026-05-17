import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from './auth.service';
import { FrontendLoggerService } from './frontend-logger.service';
import { NotificationService } from './notification.service';
import { NotificationRealtimeService } from './notification-realtime.service';

describe('NotificationRealtimeService connection guards', () => {
    it('stays disconnected when user is not authenticated', () => {
        const { service, notificationService } = setup(false, null);

        expect(service.connected()).toBe(false);
        expect(notificationService.fetchUnreadCount).not.toHaveBeenCalled();
        expect(notificationService.ensureNotificationsLoaded).not.toHaveBeenCalled();
    });

    it('does not connect when authenticated user has no token', () => {
        const { service, notificationService } = setup(true, null);

        expect(service.connected()).toBe(false);
        expect(notificationService.fetchUnreadCount).not.toHaveBeenCalled();
        expect(notificationService.ensureNotificationsLoaded).not.toHaveBeenCalled();
    });
});

function setup(
    isAuthenticated: boolean,
    token: string | null,
): {
    service: NotificationRealtimeService;
    notificationService: {
        fetchUnreadCount: ReturnType<typeof vi.fn>;
        ensureNotificationsLoaded: ReturnType<typeof vi.fn>;
    };
} {
    const authenticated = signal(isAuthenticated);
    const notificationService = {
        updateCount: vi.fn(),
        notifyNotificationsChanged: vi.fn(),
        fetchUnreadCount: vi.fn(),
        refreshNotifications: vi.fn(),
        ensureNotificationsLoaded: vi.fn(),
    };

    TestBed.configureTestingModule({
        providers: [
            NotificationRealtimeService,
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: authenticated,
                    getToken: vi.fn(() => token),
                },
            },
            { provide: NotificationService, useValue: notificationService },
            { provide: FrontendLoggerService, useValue: { warn: vi.fn(), error: vi.fn() } },
        ],
    });

    return {
        service: TestBed.inject(NotificationRealtimeService),
        notificationService,
    };
}
