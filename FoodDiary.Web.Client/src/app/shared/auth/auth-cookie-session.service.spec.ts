import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable, of, Subject, take, tap } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from '../platform/browser-window.service';
import { AuthCookieSessionService } from './auth-cookie-session.service';

let service: AuthCookieSessionService;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuthCookieSessionService);
});

afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
});

describe('AuthCookieSessionService', () => {
    it('does not start a cold cookie request before subscription', () => {
        const factory = vi.fn(() => of('login'));
        service.run(factory);
        expect(factory).not.toHaveBeenCalled();
    });

    it('serializes cookie response and identity application before the next writer starts', () => {
        const logout = new Subject<string>();
        const events: string[] = [];
        service.run(() => logout.pipe(tap(() => events.push('Alice logout applied')))).subscribe();
        service
            .run(() => {
                events.push('Bob login sent');
                return of('Bob').pipe(tap(() => events.push('Bob session applied')));
            })
            .subscribe();
        expect(events).toEqual([]);
        logout.next('done');
        expect(events).toEqual(['Alice logout applied']);
        logout.complete();
        expect(events).toEqual(['Alice logout applied', 'Bob login sent', 'Bob session applied']);
    });

    it('cancels an active HTTP writer and releases the queue without applying its identity', () => {
        const http = TestBed.inject(HttpClient);
        const requests = TestBed.inject(HttpTestingController);
        const apply = vi.fn();
        const active = service.run(() => http.post('/cookie/login', {}).pipe(tap(apply))).subscribe();
        const request = requests.expectOne('/cookie/login');
        const next = vi.fn();
        service.run(() => of('next')).subscribe(next);
        expect(next).not.toHaveBeenCalled();

        active.unsubscribe();

        expect(request.cancelled).toBe(true);
        expect(apply).not.toHaveBeenCalled();
        expect(next).toHaveBeenCalledWith('next');
    });

    it('skips a cancelled queued writer without subscribing its HTTP operation', () => {
        const first = new Subject<void>();
        service.run(() => first).subscribe();
        const factory = vi.fn(() => TestBed.inject(HttpClient).post('/cookie/cancelled', {}));
        const queued = service.run(factory).subscribe();
        queued.unsubscribe();
        first.complete();
        expect(factory).not.toHaveBeenCalled();
        TestBed.inject(HttpTestingController).expectNone('/cookie/cancelled');
    });
});

describe('AuthCookieSessionService lock completion', () => {
    it('rechecks cancellation after the origin lock is acquired', async () => {
        let acquire = (): void => {};
        const gate = new Promise<void>(resolve => {
            acquire = resolve;
        });
        vi.spyOn(TestBed.inject(BrowserWindowService), 'runWithLockAsync').mockImplementation(
            async <T>(_name: string, operation: () => Promise<T>): Promise<T> => gate.then(operation),
        );
        const factory = vi.fn(() => of('cancelled'));
        const queued = service.run(factory).subscribe();
        queued.unsubscribe();
        acquire();
        await vi.waitFor(() => {
            expect(factory).not.toHaveBeenCalled();
        });
    });

    it('releases the next writer after an error or a factory exception', () => {
        const errors = vi.fn();
        service
            .run(() => {
                throw new Error('Rejected before HTTP');
            })
            .subscribe({ error: errors });
        const next = vi.fn();
        service.run(() => of('next')).subscribe(next);
        expect(errors).toHaveBeenCalledOnce();
        expect(next).toHaveBeenCalledWith('next');
    });

    it('unsubscribes a synchronous source when a downstream consumer cancels during next', () => {
        const disposed = vi.fn();
        const source = new Observable<number>(subscriber => {
            subscriber.next(1);
            return disposed;
        });
        service
            .run(() => source)
            .pipe(take(1))
            .subscribe();
        expect(disposed).toHaveBeenCalledOnce();
    });

    it('releases a queued HTTP writer after the browser declines the earlier cookie lock', async () => {
        const denied = new DOMException('The document is not fully active', 'InvalidStateError');
        const lock = vi.spyOn(TestBed.inject(BrowserWindowService), 'runWithLockAsync').mockRejectedValueOnce(denied);
        const http = TestBed.inject(HttpClient);
        const requests = TestBed.inject(HttpTestingController);
        const restorePath = '/api/v1/auth/session';
        const loginPath = '/api/v1/auth/login';
        const rejected = expect(firstValueFrom(service.run(() => http.post(restorePath, {})))).rejects.toBe(denied);
        const next = firstValueFrom(service.run(() => http.post(loginPath, {})));
        requests.expectNone(restorePath);
        requests.expectNone(loginPath);

        await rejected;

        requests.expectNone(restorePath);
        requests.expectOne({ method: 'POST', url: loginPath }).flush({ sessionId: 'fresh-session' });
        await expect(next).resolves.toEqual({ sessionId: 'fresh-session' });
        expect(lock).toHaveBeenCalledTimes(2);
    });
});

it('cancels the active writer and advances the queue only once if its browser lock is released', async () => {
    let releaseLock: (error: DOMException) => void = () => {};
    const lockReleased = new Promise<never>((_resolve, reject) => {
        releaseLock = reject;
    });
    // A revoked browser lock can reject its request while its callback is still running.
    vi.spyOn(TestBed.inject(BrowserWindowService), 'runWithLockAsync').mockImplementationOnce(
        async <T>(_name: string, write: () => Promise<T>): Promise<T> => Promise.race([write(), lockReleased]),
    );
    const http = TestBed.inject(HttpClient);
    const requests = TestBed.inject(HttpTestingController);
    const firstPath = '/cookie/first';
    const secondPath = '/cookie/second';
    const thirdPath = '/cookie/third';
    const error = new DOMException('The active browser lock was released', 'AbortError');
    const first = expect(firstValueFrom(service.run(() => http.post(firstPath, {})))).rejects.toBe(error);
    const active = requests.expectOne({ method: 'POST', url: firstPath });
    const second = firstValueFrom(service.run(() => http.post(secondPath, {})));
    const third = firstValueFrom(service.run(() => http.post(thirdPath, {})));
    requests.expectNone(secondPath);
    requests.expectNone(thirdPath);

    releaseLock(error);
    await first;

    expect(active.cancelled).toBe(true);
    const successor = requests.expectOne({ method: 'POST', url: secondPath });
    requests.expectNone(thirdPath);

    successor.flush({ writer: 'second' });
    await expect(second).resolves.toEqual({ writer: 'second' });
    requests.expectOne({ method: 'POST', url: thirdPath }).flush({ writer: 'third' });
    await expect(third).resolves.toEqual({ writer: 'third' });
});
