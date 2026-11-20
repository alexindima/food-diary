import { DOCUMENT } from '@angular/common';
import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { AuthService } from '../../services/auth.service';
import { FrontendLoggerService } from '../../services/frontend-logger.service';
import { JwtDecoderService } from '../../services/jwt-decoder.service';
import { NotificationService } from './notification.service';
import { NotificationRealtimeService } from './notification-realtime.service';

const UNREAD_COUNT = 7;
const WAIT_ATTEMPTS = 20;
const AUTH_TOKEN = 'auth-token';
const RECONCILIATION_INTERVAL_MS = 30_000;

const signalrMock = vi.hoisted(() => {
    const handlers = new Map<string, (...args: unknown[]) => void>();
    let reconnectedHandler: (() => void) | null = null;
    let closeHandler: (() => void) | null = null;
    const connection = {
        on: vi.fn((eventName: string, handler: (...args: unknown[]) => void) => {
            handlers.set(eventName, handler);
        }),
        onclose: vi.fn((handler: (error?: Error) => void) => {
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

beforeEach(() => {
    TestBed.resetTestingModule();
    signalrMock.reset();
});

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

    it('connects with SignalR and loads notifications when authenticated token is available', async () => {
        const { service, notificationService } = setup(true, AUTH_TOKEN);

        await waitForAsync(() => signalrMock.connection.start.mock.calls.length > 0);

        expect(signalrMock.builder.withUrl).toHaveBeenCalledWith(
            expect.stringContaining('/hubs/notifications'),
            expect.objectContaining({
                accessTokenFactory: expect.any(Function) as () => Promise<string>,
            }),
        );
        const withUrlCall = signalrMock.builder.withUrl.mock.calls.at(-1) as unknown[] | undefined;
        const options = withUrlCall?.[1] as { accessTokenFactory: () => Promise<string> };
        expect(await options.accessTokenFactory()).toBe(AUTH_TOKEN);
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

    it('does not warn when the connection closes during logout', async () => {
        const { authenticated, logger, service } = setup(true, AUTH_TOKEN);
        await waitForAsync(() => service.connected());
        signalrMock.connection.stop.mockImplementationOnce(async () => {
            signalrMock.triggerClose();
            await waitForAsyncTasksAsync();
        });

        authenticated.set(false);
        TestBed.tick();
        await waitForAsync(() => signalrMock.connection.stop.mock.calls.length > 0);

        expect(service.connected()).toBe(false);
        expect(logger.warn).not.toHaveBeenCalled();
    });
});

describe('NotificationRealtimeService pending disconnect lifetime', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
        signalrMock.reset();
    });

    it('reconnects when the same user signs in while the previous connection is stopping', async () => {
        let completeStop: (() => void) | undefined;
        signalrMock.connection.stop.mockImplementationOnce(
            async () =>
                new Promise<void>(resolve => {
                    completeStop = resolve;
                }),
        );
        const { authenticated, service } = setup(true, AUTH_TOKEN);
        await waitForAsync(() => service.connected());
        try {
            authenticated.set(false);
            TestBed.tick();
            await waitForAsync(() => completeStop !== undefined);
            authenticated.set(true);
            TestBed.tick();
            completeStop?.();
            await waitForAsync(() => signalrMock.connection.start.mock.calls.length === 2);

            expect(signalrMock.connection.stop).toHaveBeenCalledOnce();
            expect(signalrMock.connection.start).toHaveBeenCalledTimes(2);
            expect(service.connected()).toBe(true);
        } finally {
            completeStop?.();
        }
    });
});

describe('Notification realtime account and credential changes', () => {
    it('starts once the session key becomes available for the same signed-in account', async () => {
        const { sessionKey, service, notificationService } = setup(true, null);
        TestBed.tick();
        await waitForAsyncTasksAsync();
        expect(signalrMock.builder.build).not.toHaveBeenCalled();
        expect(service.connected()).toBe(false);

        sessionKey.set(AUTH_TOKEN);

        await vi.waitFor(() => {
            TestBed.tick();
            expect(service.connected()).toBe(true);
        });
        expect(signalrMock.connection.start).toHaveBeenCalledOnce();
        expect(notificationService.ensureNotificationsLoaded).toHaveBeenCalledOnce();
    });

    it('keeps a new account connected when starting the previous account’s hub fails late', async () => {
        const previousStart = new Subject<void>();
        const previous = {
            ...signalrMock.connection,
            start: vi.fn(async () => firstValueFrom(previousStart)),
            stop: vi.fn(async () => {}),
        };
        signalrMock.builder.build.mockReturnValueOnce(previous);
        const { account, service, logger } = setup(true, AUTH_TOKEN);
        await vi.waitFor(() => {
            TestBed.tick();
            expect(previous.start).toHaveBeenCalledOnce();
        });

        account.set('user-b');
        await vi.waitFor(() => {
            TestBed.tick();
            expect(signalrMock.connection.start).toHaveBeenCalledOnce();
            expect(service.connected()).toBe(true);
        });
        expect(previous.stop).toHaveBeenCalledOnce();

        previousStart.error(new Error('Previous hub negotiation failed'));
        await waitForAsyncTasksAsync();

        expect(service.connected()).toBe(true);
        expect(logger.error).not.toHaveBeenCalled();
    });
});

describe('NotificationRealtimeService impersonation lifecycle', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
        signalrMock.reset();
    });

    it('does not negotiate or load notifications for an impersonated session', async () => {
        const { service, notificationService } = setup(true, AUTH_TOKEN, true);
        await waitForAsyncTasksAsync();
        TestBed.tick();
        expect(signalrMock.connection.start).not.toHaveBeenCalled();
        expect(notificationService.fetchUnreadCount).not.toHaveBeenCalled();
        expect(service.connected()).toBe(false);
    });

    it('disconnects on impersonation and reconnects when a normal authenticated session resumes', async () => {
        const { impersonating, service } = setup(true, AUTH_TOKEN);
        await waitForAsync(() => service.connected());
        const withUrlCall = signalrMock.builder.withUrl.mock.calls.at(-1) as unknown[] | undefined;
        const options = withUrlCall?.[1] as { accessTokenFactory: () => Promise<string> };
        impersonating.set(true);
        TestBed.tick();
        await waitForAsync(() => !service.connected());
        expect(signalrMock.connection.stop).toHaveBeenCalledOnce();
        expect(await options.accessTokenFactory()).toBe('');
        impersonating.set(false);
        await waitForAsync(() => service.connected());
        expect(signalrMock.connection.start).toHaveBeenCalledTimes(2);
    });

    it('does not mark a pending start connected after impersonation begins', async () => {
        let completeStart: (() => void) | undefined;
        signalrMock.connection.start.mockImplementationOnce(
            async () =>
                new Promise<void>(resolve => {
                    completeStart = resolve;
                }),
        );
        const { impersonating, service, notificationService } = setup(true, AUTH_TOKEN);
        await waitForAsync(() => completeStart !== undefined);
        impersonating.set(true);
        TestBed.tick();
        await waitForAsync(() => signalrMock.connection.stop.mock.calls.length > 0);
        completeStart?.();
        await waitForAsyncTasksAsync();
        expect(service.connected()).toBe(false);
        expect(notificationService.fetchUnreadCount).not.toHaveBeenCalled();
    });
});

describe('NotificationRealtimeService expired sessions', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
        signalrMock.reset();
    });

    it('refreshes an expired token before a reconnect uses it', async () => {
        setup(true, AUTH_TOKEN);
        const expired = vi.spyOn(TestBed.inject(JwtDecoderService), 'isExpired').mockReturnValue(true);
        const refreshedToken = 'refreshed-session';
        const refresh = vi.spyOn(TestBed.inject(AuthService), 'refreshSession').mockImplementation(() => {
            vi.spyOn(TestBed.inject(AuthService), 'getToken').mockReturnValue(refreshedToken);
            return of(true);
        });
        await waitForAsync(() => signalrMock.connection.start.mock.calls.length > 0);
        const withUrlCall = signalrMock.builder.withUrl.mock.calls.at(-1) as unknown[] | undefined;
        const options = withUrlCall?.[1] as { accessTokenFactory: () => Promise<string> };
        expect(await options.accessTokenFactory()).toBe(refreshedToken);
        expect(expired).toHaveBeenCalledWith(AUTH_TOKEN);
        expect(refresh).toHaveBeenCalledOnce();
    });

    it('does not reuse the expired token when refresh fails', async () => {
        setup(true, AUTH_TOKEN);
        vi.spyOn(TestBed.inject(JwtDecoderService), 'isExpired').mockReturnValue(true);
        vi.spyOn(TestBed.inject(AuthService), 'refreshSession').mockReturnValue(of(false));
        await waitForAsync(() => signalrMock.connection.start.mock.calls.length > 0);
        const withUrlCall = signalrMock.builder.withUrl.mock.calls.at(-1) as unknown[] | undefined;
        const options = withUrlCall?.[1] as { accessTokenFactory: () => Promise<string> };
        expect(await options.accessTokenFactory()).toBe('');
    });

    it('ignores late hub events owned by a previous account', async () => {
        const { notificationService, service } = setup(true, AUTH_TOKEN);
        await waitForAsync(() => service.connected());
        notificationService.fetchUnreadCount.mockClear();
        vi.spyOn(TestBed.inject(AuthService), 'getUserId').mockReturnValue('another-user');

        signalrMock.handlers.get('UnreadCountUpdated')?.(UNREAD_COUNT);
        signalrMock.handlers.get('NotificationsChanged')?.();
        signalrMock.triggerReconnected();

        expect(notificationService.updateCount).not.toHaveBeenCalled();
        expect(notificationService.notifyNotificationsChanged).not.toHaveBeenCalled();
        expect(notificationService.fetchUnreadCount).not.toHaveBeenCalled();
        expect(notificationService.refreshNotifications).not.toHaveBeenCalled();
    });
});

describe('NotificationRealtimeService connection tokens', () => {
    it('uses cookie authentication without supplying an access token', async () => {
        const { service } = setup(true, 'browser-session');
        vi.spyOn(TestBed.inject(AuthService), 'getToken').mockReturnValue(null);
        await waitForAsync(() => service.connected());
        const withUrlCall = signalrMock.builder.withUrl.mock.calls.at(-1) as unknown[] | undefined;
        const options = withUrlCall?.[1] as { accessTokenFactory: () => Promise<string>; headers: Record<string, string> };

        expect(options.headers).toEqual({ 'X-Requested-With': 'XMLHttpRequest' });
        await expect(options.accessTokenFactory()).resolves.toBe('');
    });

    it('withholds a new account token from the previous connection when refresh finishes after a switch', async () => {
        const { service } = setup(true, AUTH_TOKEN);
        const auth = TestBed.inject(AuthService);
        const refresh = new Subject<boolean>();
        const refreshSession = vi.spyOn(auth, 'refreshSession').mockReturnValue(refresh);
        vi.spyOn(TestBed.inject(JwtDecoderService), 'isExpired').mockReturnValue(true);
        await waitForAsync(() => service.connected());
        const withUrlCall = signalrMock.builder.withUrl.mock.calls.at(-1) as unknown[] | undefined;
        const options = withUrlCall?.[1] as { accessTokenFactory: () => Promise<string> };

        const pendingToken = options.accessTokenFactory();
        expect(refreshSession).toHaveBeenCalledOnce();
        vi.spyOn(auth, 'getUserId').mockReturnValue('user-b');
        vi.spyOn(auth, 'getToken').mockReturnValue('user-b-test-token');
        refresh.next(true);

        await expect(pendingToken).resolves.toBe('');
        await expect(options.accessTokenFactory()).resolves.toBe('');
        expect(refreshSession).toHaveBeenCalledOnce();
    });
});

describe('NotificationRealtimeService visibility reconciliation', () => {
    it('refreshes on resume, polls the visible tab and pauses the feed while it is already loading', async () => {
        const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
        const schedule = vi.spyOn(window, 'setInterval');
        const { notificationService, service } = setup(true, AUTH_TOKEN);
        const countAfterPoll = 3;
        try {
            await waitForAsync(() => service.connected());
            expect(schedule.mock.calls.some(([, delay]) => delay === RECONCILIATION_INTERVAL_MS)).toBe(false);
            expect(notificationService.fetchUnreadCount).toHaveBeenCalledExactlyOnceWith();

            schedule.mockRestore();
            vi.useFakeTimers();
            notificationService.notificationsRequested.set(true);
            visibility.mockReturnValue('visible');
            document.dispatchEvent(new Event('visibilitychange'));
            document.dispatchEvent(new Event('visibilitychange'));
            expect(notificationService.fetchUnreadCount).toHaveBeenCalledTimes(2);
            expect(notificationService.refreshNotifications).toHaveBeenCalledOnce();

            notificationService.notificationsLoading.set(true);
            await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
            expect(notificationService.fetchUnreadCount).toHaveBeenCalledTimes(countAfterPoll);
            expect(notificationService.refreshNotifications).toHaveBeenCalledOnce();

            visibility.mockReturnValue('hidden');
            document.dispatchEvent(new Event('visibilitychange'));
            await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
            expect(notificationService.fetchUnreadCount).toHaveBeenCalledTimes(countAfterPoll);
        } finally {
            TestBed.resetTestingModule();
            vi.useRealTimers();
            visibility.mockRestore();
            schedule.mockRestore();
        }
    });
});

describe('NotificationRealtimeService stale connection outcomes', () => {
    it('ignores the previous hub’s close event after connecting the current account', async () => {
        signalrMock.builder.build.mockReturnValueOnce({ ...signalrMock.connection });
        const { account, logger, service } = setup(true, AUTH_TOKEN);
        await waitForAsync(() => service.connected());
        const [closePrevious] = signalrMock.connection.onclose.mock.calls[0];
        account.set('user-b');
        await waitForAsync(() => service.connected() && signalrMock.connection.start.mock.calls.length === 2);

        closePrevious(new Error('Previous hub closed'));

        expect(signalrMock.connection.start).toHaveBeenCalledTimes(2);
        expect(service.connected()).toBe(true);
        expect(logger.warn).not.toHaveBeenCalled();
    });

    it('drops the old negotiation when impersonation starts before the hub is constructed', async () => {
        const { impersonating, service } = setup(true, AUTH_TOKEN);
        TestBed.tick();
        impersonating.set(true);
        TestBed.tick();
        await import('@microsoft/signalr');
        await waitForAsyncTasksAsync();

        expect(signalrMock.builder.build).not.toHaveBeenCalled();
        expect(service.connected()).toBe(false);

        impersonating.set(false);
        await waitForAsync(() => service.connected());
        expect(signalrMock.connection.start).toHaveBeenCalledOnce();
    });

    it.each(['account', 'session key'] as const)(
        'drops a queued reconnect if the %s disappears before the previous hub stops',
        async field => {
            const closed = new Subject<void>();
            signalrMock.connection.stop.mockImplementationOnce(async () => firstValueFrom(closed));
            const { authenticated, service, sessionKey } = setup(true, AUTH_TOKEN);
            await waitForAsync(() => service.connected());

            try {
                authenticated.set(false);
                TestBed.tick();
                expect(signalrMock.connection.stop).toHaveBeenCalledOnce();
                authenticated.set(true);
                TestBed.tick();
                if (field === 'account') {
                    authenticated.set(false);
                } else {
                    sessionKey.set(null);
                }
                TestBed.tick();

                closed.next();
                await waitForAsync(() => !service.connected());
                await waitForAsyncTasksAsync();
                expect(signalrMock.builder.build).toHaveBeenCalledOnce();
                expect(service.connected()).toBe(false);

                authenticated.set(true);
                sessionKey.set('new-session-key');
                await waitForAsync(() => service.connected());
                expect(signalrMock.connection.start).toHaveBeenCalledTimes(2);
            } finally {
                closed.next();
                closed.complete();
            }
        },
    );
});

describe('NotificationRealtimeService view and refreshed credentials', () => {
    it('does not schedule a reconciliation timer when the document has no browsing view', async () => {
        const detached = document.implementation.createHTMLDocument();
        const schedule = vi.spyOn(window, 'setInterval');
        try {
            const { service, notificationService } = setup(true, AUTH_TOKEN, false, detached);
            await waitForAsync(() => service.connected());

            expect(service.connected()).toBe(true);
            expect(notificationService.fetchUnreadCount).toHaveBeenCalledExactlyOnceWith();
            expect(schedule.mock.calls.some(([, delay]) => delay === RECONCILIATION_INTERVAL_MS)).toBe(false);
        } finally {
            schedule.mockRestore();
        }
    });

    it('skips a queued reconciliation tick after visibility changes and before the event is delivered', async () => {
        const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
        try {
            const { notificationService, service } = setup(true, AUTH_TOKEN);
            await waitForAsync(() => service.connected());
            vi.useFakeTimers();
            visibility.mockReturnValue('visible');
            document.dispatchEvent(new Event('visibilitychange'));
            notificationService.fetchUnreadCount.mockClear();
            visibility.mockReturnValue('hidden');

            await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);

            expect(notificationService.fetchUnreadCount).not.toHaveBeenCalled();
        } finally {
            TestBed.resetTestingModule();
            vi.useRealTimers();
            visibility.mockRestore();
        }
    });

    it('does not send the expired bearer when refresh restores a cookie-only session', async () => {
        const { service } = setup(true, AUTH_TOKEN);
        const auth = TestBed.inject(AuthService);
        const token = vi.spyOn(auth, 'getToken');
        vi.spyOn(TestBed.inject(JwtDecoderService), 'isExpired').mockReturnValue(true);
        const refresh = vi.spyOn(auth, 'refreshSession').mockImplementation(() => {
            token.mockReturnValue(null);
            return of(true);
        });
        await waitForAsync(() => service.connected());
        const withUrlCall = signalrMock.builder.withUrl.mock.calls.at(-1) as unknown[] | undefined;
        const options = withUrlCall?.[1] as { accessTokenFactory: () => Promise<string> };

        await expect(options.accessTokenFactory()).resolves.toBe('');
        expect(refresh).toHaveBeenCalledOnce();
    });
});

it('reconnects when the initial start fails while the same account’s previous hub is stopping', async () => {
    const starting = new Subject<void>();
    const stopping = new Subject<void>();
    signalrMock.connection.start.mockImplementationOnce(async () => firstValueFrom(starting));
    signalrMock.connection.stop.mockImplementationOnce(async () => firstValueFrom(stopping));
    const { authenticated, logger, service } = setup(true, AUTH_TOKEN);
    const error = new Error('Previous start failed during stop');

    try {
        await waitForAsync(() => signalrMock.connection.start.mock.calls.length === 1);
        authenticated.set(false);
        TestBed.tick();
        expect(signalrMock.connection.stop).toHaveBeenCalledOnce();
        authenticated.set(true);
        TestBed.tick();
        starting.error(error);
        await waitForAsync(() => logger.error.mock.calls.length > 0);
        expect(logger.error).toHaveBeenCalledWith('Notification SignalR connection failed', error, { devOnly: true });

        stopping.next();
        await waitForAsync(() => service.connected());
        expect(signalrMock.connection.start).toHaveBeenCalledTimes(2);
        expect(service.connected()).toBe(true);
    } finally {
        starting.complete();
        stopping.next();
        stopping.complete();
    }
});

function setup(
    isAuthenticated: boolean,
    token: string | null,
    isImpersonating = false,
    documentOverride?: Document,
): {
    authenticated: WritableSignal<boolean>;
    impersonating: WritableSignal<boolean>;
    account: WritableSignal<string>;
    sessionKey: WritableSignal<string | null>;
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
        notificationsRequested: WritableSignal<boolean>;
        notificationsLoading: WritableSignal<boolean>;
    };
} {
    const authenticated = signal(isAuthenticated);
    const impersonating = signal(isImpersonating);
    const account = signal('user-a');
    const sessionKey = signal(token);
    const notificationService = {
        updateCount: vi.fn(),
        notifyNotificationsChanged: vi.fn(),
        fetchUnreadCount: vi.fn(),
        refreshNotifications: vi.fn(),
        ensureNotificationsLoaded: vi.fn(),
        notificationsRequested: signal(false),
        notificationsLoading: signal(false),
    };
    const logger = { warn: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
        providers: [
            NotificationRealtimeService,
            ...(documentOverride === undefined ? [] : [{ provide: DOCUMENT, useValue: documentOverride }]),
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: authenticated,
                    isImpersonating: impersonating,
                    getUserId: vi.fn(() => account()),
                    getToken: vi.fn(() => sessionKey()),
                    getSessionKey: vi.fn(() => sessionKey()),
                    sessionHubUrl: (hubUrl: string): string => hubUrl,
                    refreshSession: vi.fn(() => of(false)),
                },
            },
            { provide: NotificationService, useValue: notificationService },
            { provide: FrontendLoggerService, useValue: logger },
        ],
    });

    return {
        authenticated,
        impersonating,
        account,
        sessionKey,
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
