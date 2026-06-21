import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { AuthService } from '../../services/auth.service';
import { FrontendLoggerService } from '../../services/frontend-logger.service';
import { NotificationService } from './notification.service';
import { NotificationRealtimeService } from './notification-realtime.service';

const UNREAD_COUNT = 7;
const WAIT_ATTEMPTS = 20;
const AUTH_TOKEN = 'auth-token';

const signalrMock = vi.hoisted(() => {
    const handlers = new Map<string, (...args: unknown[]) => void>();
    let reconnectedHandler: (() => void) | null = null;
    let closeHandler: (() => void) | null = null;
    const connection = {
        on: vi.fn((eventName: string, handler: (...args: unknown[]) => void) => {
            handlers.set(eventName, handler);
        }),
        onclose: vi.fn((handler: () => void) => {
            closeHandler = handler;
        }),
        onreconnected: vi.fn((handler: () => void) => {
            reconnectedHandler = handler;
        }),
        start: vi.fn(async () => {}),
        stop: vi.fn(async () => {}),
    };
    const builder = {
        build: vi.fn(() => connection),
        configureLogging: vi.fn(() => builder),
        withAutomaticReconnect: vi.fn(() => builder),
        withUrl: vi.fn(() => builder),
    };
    function HubConnectionBuilder(): typeof builder {
        return builder;
    }

    return {
        HubConnectionBuilder,
        builder,
        connection,
        handlers,
        reset: (): void => {
            handlers.clear();
            reconnectedHandler = null;
            closeHandler = null;
            builder.withUrl.mockClear();
            builder.withAutomaticReconnect.mockClear();
            builder.configureLogging.mockClear();
            builder.build.mockClear();
            connection.on.mockClear();
            connection.onreconnected.mockClear();
            connection.onclose.mockClear();
            connection.start.mockReset().mockResolvedValue(void 0);
            connection.stop.mockReset().mockResolvedValue(void 0);
        },
        triggerClose: (): void => {
            closeHandler?.();
        },
        triggerReconnected: (): void => {
            reconnectedHandler?.();
        },
    };
});

vi.mock('@microsoft/signalr', () => ({
    HubConnectionBuilder: signalrMock.HubConnectionBuilder,
    LogLevel: {
        Warning: 'Warning',
    },
}));

describe('NotificationRealtimeService connection guards', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
        signalrMock.reset();
    });

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

    it('connects with SignalR and loads notifications when authenticated token is available', async () => {
        const { service, notificationService } = setup(true, AUTH_TOKEN);

        await waitForAsync(() => signalrMock.connection.start.mock.calls.length > 0);

        expect(signalrMock.builder.withUrl).toHaveBeenCalledWith(
            expect.stringContaining('/hubs/notifications'),
            expect.objectContaining({
                accessTokenFactory: expect.any(Function) as () => string,
            }),
        );
        const withUrlCall = signalrMock.builder.withUrl.mock.calls.at(-1) as unknown[] | undefined;
        const options = withUrlCall?.[1] as { accessTokenFactory: () => string };
        expect(options.accessTokenFactory()).toBe(AUTH_TOKEN);
        expect(service.connected()).toBe(true);
        expect(notificationService.fetchUnreadCount).toHaveBeenCalledOnce();
        expect(notificationService.ensureNotificationsLoaded).toHaveBeenCalledOnce();
    });

    it('forwards SignalR hub events to notification service', async () => {
        const { notificationService } = setup(true, AUTH_TOKEN);

        await waitForAsync(() => signalrMock.handlers.has('UnreadCountUpdated'));
        signalrMock.handlers.get('UnreadCountUpdated')?.(UNREAD_COUNT);
        signalrMock.handlers.get('NotificationsChanged')?.();

        expect(notificationService.updateCount).toHaveBeenCalledWith(UNREAD_COUNT);
        expect(notificationService.notifyNotificationsChanged).toHaveBeenCalledOnce();
    });

    it('refreshes notifications after reconnect and marks connection closed on close', async () => {
        const { logger, notificationService, service } = setup(true, AUTH_TOKEN);

        await waitForAsync(() => service.connected());
        signalrMock.triggerReconnected();

        expect(notificationService.fetchUnreadCount).toHaveBeenLastCalledWith({ force: true });
        expect(notificationService.refreshNotifications).toHaveBeenCalledOnce();
        expect(service.connected()).toBe(true);

        signalrMock.triggerClose();

        expect(service.connected()).toBe(false);
        expect(logger.warn).toHaveBeenCalledWith('Notification SignalR connection closed', void 0, { devOnly: true });
    });

    it('logs failed connection attempts and stays disconnected', async () => {
        const connectionError = new Error('connect failed');
        signalrMock.connection.start.mockRejectedValueOnce(connectionError);
        const { logger, service } = setup(true, AUTH_TOKEN);

        await waitForAsync(() => logger.error.mock.calls.length > 0);

        expect(service.connected()).toBe(false);
        expect(logger.error).toHaveBeenCalledWith('Notification SignalR connection failed', connectionError, { devOnly: true });
    });
});

function setup(
    isAuthenticated: boolean,
    token: string | null,
): {
    logger: {
        error: ReturnType<typeof vi.fn>;
        warn: ReturnType<typeof vi.fn>;
    };
    service: NotificationRealtimeService;
    notificationService: {
        updateCount: ReturnType<typeof vi.fn>;
        notifyNotificationsChanged: ReturnType<typeof vi.fn>;
        fetchUnreadCount: ReturnType<typeof vi.fn>;
        refreshNotifications: ReturnType<typeof vi.fn>;
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
    const logger = { warn: vi.fn(), error: vi.fn() };

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
            { provide: FrontendLoggerService, useValue: logger },
        ],
    });

    return {
        logger,
        service: TestBed.inject(NotificationRealtimeService),
        notificationService,
    };
}

async function waitForAsync(predicate: () => boolean): Promise<void> {
    for (let attempt = 0; attempt < WAIT_ATTEMPTS; attempt++) {
        TestBed.tick();

        if (predicate()) {
            return;
        }

        await waitForAsyncTasksAsync();
    }
}
