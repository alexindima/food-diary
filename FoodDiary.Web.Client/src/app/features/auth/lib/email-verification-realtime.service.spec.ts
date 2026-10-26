import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../services/auth.service';
import { EmailVerificationRealtimeService, toEmailVerificationHubUrl } from './email-verification-realtime.service';

const hub = vi.hoisted(() => {
    const handlers = new Map<string, () => void>();
    const connection = {
        on: vi.fn((event: string, handler: () => void) => handlers.set(event, handler)),
        start: vi.fn<() => Promise<void>>(),
        stop: vi.fn<() => Promise<void>>(),
    };
    const withUrl = vi.fn<(url: string, options: { accessTokenFactory: () => string }) => void>();
    const reconnect = vi.fn();
    const logging = vi.fn<(level: string) => void>();
    const build = vi.fn(() => connection);

    class HubConnectionBuilder {
        public withUrl(url: string, options: { accessTokenFactory: () => string }): this {
            withUrl(url, options);
            return this;
        }

        public withAutomaticReconnect(): this {
            reconnect();
            return this;
        }

        public configureLogging(level: string): this {
            logging(level);
            return this;
        }

        public build(): typeof connection {
            return build();
        }
    }

    return { HubConnectionBuilder, connection, handlers, withUrl, reconnect, logging, build };
});

vi.mock('@microsoft/signalr', () => ({ HubConnectionBuilder: hub.HubConnectionBuilder, LogLevel: { Warning: 'Warning' } }));

let service: EmailVerificationRealtimeService;
let auth: AuthService;
let getToken: ReturnType<typeof vi.fn<AuthService['getToken']>>;

beforeEach(() => {
    vi.clearAllMocks();
    hub.handlers.clear();
    hub.build.mockReset().mockReturnValue(hub.connection);
    hub.connection.start.mockReset().mockResolvedValue(undefined);
    hub.connection.stop.mockReset().mockResolvedValue(undefined);
    getToken = vi.fn().mockReturnValue('current-session-token');
    TestBed.configureTestingModule({
        providers: [
            EmailVerificationRealtimeService,
            {
                provide: AuthService,
                useValue: { getToken, isAuthenticated: vi.fn(() => true), sessionHubUrl: (hubUrl: string): string => hubUrl },
            },
        ],
    });
    service = TestBed.inject(EmailVerificationRealtimeService);
    auth = TestBed.inject(AuthService);
});

afterEach(async () => {
    hub.connection.stop.mockResolvedValue(undefined);
    await service.disconnectAsync();
});

describe('toEmailVerificationHubUrl', () => {
    it('should build the email verification hub URL from a versioned auth API base URL', () => {
        expect(toEmailVerificationHubUrl('https://fooddiary.club/api/v1/auth')).toBe('https://fooddiary.club/hubs/email-verification');
    });

    it('should build the email verification hub URL from a non-versioned auth API base URL', () => {
        expect(toEmailVerificationHubUrl('http://localhost:5300/api/auth')).toBe('http://localhost:5300/hubs/email-verification');
    });
});

describe('EmailVerificationRealtimeService connection and verification', () => {
    it.each([null, '', '   '])('stays disconnected without an authenticated session (%j)', async token => {
        getToken.mockReturnValue(token);
        vi.spyOn(auth, 'isAuthenticated').mockReturnValue(false);
        const onVerified = vi.fn();

        await service.connectAsync(auth, onVerified);

        expect(service.connected()).toBe(false);
        expect(hub.build).not.toHaveBeenCalled();
        expect(hub.connection.start).not.toHaveBeenCalled();
        expect(onVerified).not.toHaveBeenCalled();
    });

    it('connects to the verification hub with reconnect enabled and forwards the verified event', async () => {
        const onVerified = vi.fn();

        await service.connectAsync(auth, onVerified);

        expect(hub.withUrl).toHaveBeenCalledOnce();
        expect(hub.withUrl.mock.calls.at(0)?.[0]).toBe(toEmailVerificationHubUrl(environment.apiUrls.auth));
        expect(hub.withUrl.mock.calls.at(0)?.[1].accessTokenFactory).toBeTypeOf('function');
        expect(hub.reconnect).toHaveBeenCalledOnce();
        expect(hub.logging).toHaveBeenCalledExactlyOnceWith('Warning');
        expect(service.connected()).toBe(true);
        expect(onVerified).not.toHaveBeenCalled();
        expect(hub.connection.on).toHaveBeenCalledExactlyOnceWith('EmailVerified', expect.any(Function));

        hub.handlers.get('EmailVerified')?.();

        expect(onVerified).toHaveBeenCalledOnce();
    });

    it('reads the latest token for reconnect instead of keeping the token used at initial connection', async () => {
        await service.connectAsync(auth, vi.fn());
        const options = hub.withUrl.mock.calls.at(0)?.[1];
        if (options === undefined) {
            throw new Error('The hub must be configured with an access-token factory.');
        }

        expect(options.accessTokenFactory()).toBe('current-session-token');
        getToken.mockReturnValue('renewed-session-token');
        expect(options.accessTokenFactory()).toBe('renewed-session-token');
        getToken.mockReturnValue(null);
        expect(options.accessTokenFactory()).toBe('');
        expect(hub.build).toHaveBeenCalledOnce();
    });
});

describe('EmailVerificationRealtimeService asynchronous connection', () => {
    it('announces connection only after start completes and ignores duplicate connects while it is pending', async () => {
        const starting = new Subject<void>();
        const release = new Subject<void>();
        const started = firstValueFrom(starting);
        hub.connection.start.mockImplementation(async () => {
            starting.next();
            await firstValueFrom(release);
        });
        const onVerified = vi.fn();
        const duplicateCallback = vi.fn();
        const connection = service.connectAsync(auth, onVerified);
        await started;

        expect(service.connected()).toBe(false);
        await service.connectAsync(auth, duplicateCallback);
        expect(hub.build).toHaveBeenCalledOnce();
        expect(hub.connection.start).toHaveBeenCalledOnce();

        release.next();
        release.complete();
        await connection;
        expect(service.connected()).toBe(true);
        await service.connectAsync(auth, duplicateCallback);
        hub.handlers.get('EmailVerified')?.();
        expect(onVerified).toHaveBeenCalledOnce();
        expect(duplicateCallback).not.toHaveBeenCalled();
        expect(hub.build).toHaveBeenCalledOnce();
    });

    it('keeps a failed start nonfatal and can connect again after explicit cleanup', async () => {
        const onVerified = vi.fn();
        hub.connection.start.mockRejectedValueOnce(new Error('Hub unavailable'));

        await expect(service.connectAsync(auth, onVerified)).resolves.toBeUndefined();

        expect(service.connected()).toBe(false);
        expect(onVerified).not.toHaveBeenCalled();
        await service.disconnectAsync();
        await service.connectAsync(auth, onVerified);
        expect(service.connected()).toBe(true);
        expect(hub.build).toHaveBeenCalledTimes(2);
        expect(hub.connection.start).toHaveBeenCalledTimes(2);
    });
});

describe('EmailVerificationRealtimeService cleanup', () => {
    it('does not call the transport when disconnecting a service that never connected', async () => {
        await service.disconnectAsync();

        expect(hub.connection.stop).not.toHaveBeenCalled();
        expect(service.connected()).toBe(false);
    });

    it('awaits transport shutdown, clears connection state and permits a fresh connection', async () => {
        await service.connectAsync(auth, vi.fn());
        const release = new Subject<void>();
        hub.connection.stop.mockReturnValue(firstValueFrom(release));
        const disconnect = service.disconnectAsync();

        expect(hub.connection.stop).toHaveBeenCalledOnce();
        expect(service.connected()).toBe(true);
        release.next();
        release.complete();
        await disconnect;
        expect(service.connected()).toBe(false);

        await service.connectAsync(auth, vi.fn());
        expect(hub.build).toHaveBeenCalledTimes(2);
        expect(service.connected()).toBe(true);
    });

    it('clears a rejected shutdown so the next connection is not blocked by a dead transport', async () => {
        await service.connectAsync(auth, vi.fn());
        const failure = new Error('Transport stop failed');
        hub.connection.stop.mockRejectedValueOnce(failure);

        await expect(service.disconnectAsync()).rejects.toBe(failure);

        expect(service.connected()).toBe(false);
        await service.connectAsync(auth, vi.fn());
        expect(hub.build).toHaveBeenCalledTimes(2);
        expect(service.connected()).toBe(true);
    });
});

describe('EmailVerificationRealtimeService lazy connection races', () => {
    it('shares the connection attempt when two callers connect before the SignalR import completes', async () => {
        const onVerified = vi.fn();
        const duplicateCallback = vi.fn();

        const first = service.connectAsync(auth, onVerified);
        const second = service.connectAsync(auth, duplicateCallback);
        expect(hub.build).not.toHaveBeenCalled();
        await Promise.all([first, second]);

        expect(hub.build).toHaveBeenCalledOnce();
        expect(hub.connection.start).toHaveBeenCalledOnce();
        hub.handlers.get('EmailVerified')?.();
        expect(onVerified).toHaveBeenCalledOnce();
        expect(duplicateCallback).not.toHaveBeenCalled();
    });

    it('cancels an attempt disconnected before the lazy import completes without opening a transport', async () => {
        const onVerified = vi.fn();
        const connecting = service.connectAsync(auth, onVerified);

        await service.disconnectAsync();
        await connecting;

        expect(hub.build).not.toHaveBeenCalled();
        expect(hub.connection.start).not.toHaveBeenCalled();
        expect(hub.connection.stop).not.toHaveBeenCalled();
        expect(service.connected()).toBe(false);
        expect(onVerified).not.toHaveBeenCalled();
        await service.connectAsync(auth, onVerified);
        expect(hub.build).toHaveBeenCalledOnce();
        expect(service.connected()).toBe(true);
    });
});

describe('EmailVerificationRealtimeService obsolete transport races', () => {
    it('does not announce a connection when its start completes after disconnection', async () => {
        const starting = new Subject<void>();
        const release = new Subject<void>();
        const started = firstValueFrom(starting);
        hub.connection.start.mockImplementation(async () => {
            starting.next();
            await firstValueFrom(release);
        });
        const connecting = service.connectAsync(auth, vi.fn());
        await started;

        await service.disconnectAsync();
        release.next();
        release.complete();
        await connecting;

        expect(hub.connection.stop).toHaveBeenCalledOnce();
        expect(service.connected()).toBe(false);
    });

    it('ignores an event retained by the stopped transport after a new account starts listening', async () => {
        const oldCallback = vi.fn();
        await service.connectAsync(auth, oldCallback);
        const oldEvent = hub.handlers.get('EmailVerified');
        expect(oldEvent).toBeTypeOf('function');
        await service.disconnectAsync();
        const currentCallback = vi.fn();
        await service.connectAsync(auth, currentCallback);

        oldEvent?.();
        expect(oldCallback).not.toHaveBeenCalled();
        expect(currentCallback).not.toHaveBeenCalled();
        hub.handlers.get('EmailVerified')?.();
        expect(currentCallback).toHaveBeenCalledOnce();
        expect(service.connected()).toBe(true);
    });

    it('keeps a new connection healthy when an obsolete start later fails', async () => {
        const starting = new Subject<void>();
        const release = new Subject<void>();
        const started = firstValueFrom(starting);
        hub.connection.start.mockImplementationOnce(async () => {
            starting.next();
            await firstValueFrom(release);
        });
        const previous = service.connectAsync(auth, vi.fn());
        await started;
        await service.disconnectAsync();
        await service.connectAsync(auth, vi.fn());
        expect(service.connected()).toBe(true);

        release.error(new Error('Obsolete connection failed'));
        await previous;

        expect(service.connected()).toBe(true);
        await service.connectAsync(auth, vi.fn());
        expect(hub.build).toHaveBeenCalledTimes(2);
    });
});

describe('EmailVerificationRealtimeService overlapping shutdowns', () => {
    it('does not clear a replacement connection when an earlier shutdown finishes late', async () => {
        await service.connectAsync(auth, vi.fn());
        const release = new Subject<void>();
        hub.connection.stop.mockReturnValueOnce(firstValueFrom(release));
        const earlierShutdown = service.disconnectAsync();
        await service.disconnectAsync();
        const currentCallback = vi.fn();
        await service.connectAsync(auth, currentCallback);

        release.next();
        release.complete();
        await earlierShutdown;

        expect(service.connected()).toBe(true);
        await service.connectAsync(auth, vi.fn());
        expect(hub.build).toHaveBeenCalledTimes(2);
        hub.handlers.get('EmailVerified')?.();
        expect(currentCallback).toHaveBeenCalledOnce();
    });
});
