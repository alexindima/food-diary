import { DOCUMENT } from '@angular/common';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { FrontendLoggerService } from '../../services/frontend-logger.service';
import { NotificationService } from './notification.service';
import { NotificationRealtimeService } from './notification-realtime.service';

const RECONCILIATION_INTERVAL_MS = 30_000;
const MISSED_INTERVALS = 3;
const signalrMock = vi.hoisted(() => {
    const connection = { on: vi.fn(), onclose: vi.fn(), onreconnected: vi.fn(), start: vi.fn(), stop: vi.fn() };
    const builder = {
        withUrl: vi.fn(() => builder),
        withAutomaticReconnect: vi.fn(() => builder),
        configureLogging: vi.fn(() => builder),
        build: vi.fn(() => connection),
    };
    return { connection, builder };
});

vi.mock('@microsoft/signalr', () => {
    function HubConnectionBuilder(): typeof signalrMock.builder {
        return signalrMock.builder;
    }
    return { HubConnectionBuilder, LogLevel: { Warning: 'Warning' } };
});

beforeEach(() => {
    TestBed.resetTestingModule();
    vi.useFakeTimers();
    vi.clearAllMocks();
    signalrMock.connection.start.mockResolvedValue(void 0);
    signalrMock.connection.stop.mockResolvedValue(void 0);
});

afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
});

describe('NotificationRealtimeService reconciliation', () => {
    it('reconciles committed notifications even while SignalR remains connected without an event', async () => {
        const { service, notifications } = await setupAsync();
        expect(service.connected()).toBe(true);

        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS - 1);
        expect(notifications.fetchUnreadCount).not.toHaveBeenCalled();
        await vi.advanceTimersByTimeAsync(1);

        expect(notifications.fetchUnreadCount).toHaveBeenCalledExactlyOnceWith({ force: true });
        expect(notifications.refreshNotifications).toHaveBeenCalledOnce();
        expect(notifications.ensureNotificationsLoaded).not.toHaveBeenCalled();
    });

    it('continues reconciling when the hub cannot connect', async () => {
        signalrMock.connection.start.mockRejectedValue(new Error('offline hub'));
        const { service, notifications } = await setupAsync();
        expect(service.connected()).toBe(false);

        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS * 2);

        expect(notifications.fetchUnreadCount).toHaveBeenCalledTimes(2);
        expect(notifications.refreshNotifications).toHaveBeenCalledTimes(2);
    });

    it('keeps its polling deadline when the same account rotates its access token', async () => {
        const { authToken, notifications } = await setupAsync();

        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS - 1);
        authToken.set('rotated-token');
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(1);

        expect(notifications.fetchUnreadCount).toHaveBeenCalledExactlyOnceWith({ force: true });
        expect(notifications.refreshNotifications).toHaveBeenCalledOnce();
    });

    it('does not load an unopened feed or overlap an active feed request', async () => {
        const { notifications } = await setupAsync();
        notifications.notificationsLoaded.set(false);
        notifications.notificationsRequested.set(false);
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        expect(notifications.refreshNotifications).not.toHaveBeenCalled();

        notifications.notificationsLoaded.set(true);
        notifications.notificationsRequested.set(true);
        notifications.notificationsLoading.set(true);
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        expect(notifications.refreshNotifications).not.toHaveBeenCalled();

        notifications.notificationsLoading.set(false);
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        expect(notifications.fetchUnreadCount).toHaveBeenCalledTimes(MISSED_INTERVALS);
        expect(notifications.refreshNotifications).toHaveBeenCalledOnce();
    });
});

describe('NotificationRealtimeService first feed failure recovery', () => {
    it('recovers an opened feed on the next polling interval after its first HTTP request fails', async () => {
        signalrMock.connection.start.mockRejectedValue(new Error('offline hub'));
        const baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');
        TestBed.configureTestingModule({
            providers: [
                NotificationRealtimeService,
                NotificationService,
                provideHttpClient(),
                provideHttpClientTesting(),
                {
                    provide: DOCUMENT,
                    useValue: Object.assign(new EventTarget(), {
                        visibilityState: 'visible',
                        defaultView: { setInterval, clearInterval },
                    }),
                },
                { provide: PLATFORM_ID, useValue: 'browser' },
                {
                    provide: AuthService,
                    useValue: {
                        isAuthenticated: signal(true),
                        isImpersonating: signal(false),
                        getUserId: signal('user-a'),
                        getToken: signal('token'),
                        getSessionKey: signal('session'),
                        sessionHubUrl: (hubUrl: string): string => hubUrl,
                    },
                },
                { provide: FrontendLoggerService, useValue: { error: vi.fn(), warn: vi.fn() } },
            ],
        });
        TestBed.inject(NotificationRealtimeService);
        const notifications = TestBed.inject(NotificationService);
        const http = TestBed.inject(HttpTestingController);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        notifications.ensureNotificationsLoaded();
        http.expectOne(baseUrl).flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'unavailable' });
        expect(notifications.notificationsLoaded()).toBe(false);

        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        http.expectOne(`${baseUrl}/unread-count`).flush({ count: 1 });
        http.expectOne(baseUrl).flush([
            { id: 'recovered', type: 'info', title: 'Recovered', isRead: false, createdAtUtc: '2026-10-22T10:00:00Z' },
        ]);

        expect(notifications.notifications().map(item => item.title)).toEqual(['Recovered']);
        expect(notifications.notificationsLoaded()).toBe(true);
        expect(notifications.unreadCount()).toBe(1);
        http.verify();
    });
});

describe('NotificationRealtimeService visibility reconciliation', () => {
    it('pauses in a hidden tab and reconciles once immediately when it becomes visible', async () => {
        const { notifications, documentRef } = await setupAsync();
        documentRef.visibilityState = 'hidden';
        documentRef.dispatchEvent(new Event('visibilitychange'));
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS * MISSED_INTERVALS);
        expect(notifications.fetchUnreadCount).not.toHaveBeenCalled();

        documentRef.visibilityState = 'visible';
        documentRef.dispatchEvent(new Event('visibilitychange'));
        documentRef.dispatchEvent(new Event('visibilitychange'));
        expect(notifications.fetchUnreadCount).toHaveBeenCalledOnce();
        expect(notifications.refreshNotifications).toHaveBeenCalledOnce();

        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS - 1);
        expect(notifications.fetchUnreadCount).toHaveBeenCalledOnce();
        await vi.advanceTimersByTimeAsync(1);
        expect(notifications.fetchUnreadCount).toHaveBeenCalledTimes(2);
    });

    it('retries an already opened feed after a failed refresh clears its loaded flag', async () => {
        const { notifications } = await setupAsync();
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        notifications.notificationsLoaded.set(false);
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);

        expect(notifications.fetchUnreadCount).toHaveBeenCalledTimes(2);
        expect(notifications.refreshNotifications).toHaveBeenCalledTimes(2);
    });
});

describe('NotificationRealtimeService reconciliation lifetime', () => {
    it.each([{ authenticated: false }, { impersonating: true }, { token: null }, { token: ' ' }, { platform: 'server' }])(
        'does not reconcile outside an active browser session: %j',
        async options => {
            const { notifications } = await setupAsync(options);
            await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS * 2);

            expect(notifications.fetchUnreadCount).not.toHaveBeenCalled();
            expect(notifications.refreshNotifications).not.toHaveBeenCalled();
            expect(signalrMock.connection.start).not.toHaveBeenCalled();
        },
    );

    it('stops polling on logout and impersonation, then resumes for a normal session', async () => {
        const { authenticated, impersonating, notifications } = await setupAsync();
        authenticated.set(false);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        expect(notifications.fetchUnreadCount).not.toHaveBeenCalled();

        authenticated.set(true);
        impersonating.set(true);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        expect(notifications.fetchUnreadCount).not.toHaveBeenCalled();

        impersonating.set(false);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        notifications.fetchUnreadCount.mockClear();
        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        expect(notifications.fetchUnreadCount).toHaveBeenCalledExactlyOnceWith({ force: true });
    });

    it('removes timers and visibility listeners when destroyed', async () => {
        const { notifications, documentRef } = await setupAsync();
        TestBed.resetTestingModule();

        await vi.advanceTimersByTimeAsync(RECONCILIATION_INTERVAL_MS);
        documentRef.visibilityState = 'hidden';
        documentRef.dispatchEvent(new Event('visibilitychange'));
        documentRef.visibilityState = 'visible';
        documentRef.dispatchEvent(new Event('visibilitychange'));

        expect(notifications.fetchUnreadCount).not.toHaveBeenCalled();
        expect(notifications.refreshNotifications).not.toHaveBeenCalled();
    });
});

async function setupAsync(
    options: { authenticated?: boolean; impersonating?: boolean; token?: string | null; platform?: string } = {},
): Promise<{
    service: NotificationRealtimeService;
    authenticated: ReturnType<typeof signal<boolean>>;
    impersonating: ReturnType<typeof signal<boolean>>;
    authToken: ReturnType<typeof signal<string | null>>;
    documentRef: EventTarget & { visibilityState: string };
    notifications: {
        fetchUnreadCount: ReturnType<typeof vi.fn>;
        refreshNotifications: ReturnType<typeof vi.fn>;
        ensureNotificationsLoaded: ReturnType<typeof vi.fn>;
        notificationsLoaded: ReturnType<typeof signal<boolean>>;
        notificationsRequested: ReturnType<typeof signal<boolean>>;
        notificationsLoading: ReturnType<typeof signal<boolean>>;
    };
}> {
    const authenticated = signal(options.authenticated ?? true);
    const impersonating = signal(options.impersonating ?? false);
    const authToken = signal(options.token === undefined ? 'token' : options.token);
    const notifications = {
        fetchUnreadCount: vi.fn(),
        refreshNotifications: vi.fn(),
        ensureNotificationsLoaded: vi.fn(),
        notificationsLoaded: signal(true),
        notificationsRequested: signal(true),
        notificationsLoading: signal(false),
    };
    const documentRef = Object.assign(new EventTarget(), {
        visibilityState: 'visible',
        defaultView: {
            setInterval: (callback: () => void, ms: number) => setInterval(callback, ms),
            clearInterval: (id: ReturnType<typeof setInterval>) => {
                clearInterval(id);
            },
        },
    });
    TestBed.configureTestingModule({
        providers: [
            NotificationRealtimeService,
            { provide: DOCUMENT, useValue: documentRef },
            { provide: PLATFORM_ID, useValue: options.platform ?? 'browser' },
            { provide: NotificationService, useValue: notifications },
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: authenticated,
                    isImpersonating: impersonating,
                    getUserId: vi.fn(() => 'user-a'),
                    getToken: authToken,
                    getSessionKey: authToken,
                    sessionHubUrl: (hubUrl: string): string => hubUrl,
                    refreshSession: vi.fn(() => of(null)),
                },
            },
            { provide: FrontendLoggerService, useValue: { error: vi.fn(), warn: vi.fn() } },
        ],
    });
    const service = TestBed.inject(NotificationRealtimeService);
    TestBed.tick();
    await vi.advanceTimersByTimeAsync(0);
    notifications.fetchUnreadCount.mockClear();
    notifications.ensureNotificationsLoaded.mockClear();
    return { service, notifications, authenticated, impersonating, authToken, documentRef };
}
