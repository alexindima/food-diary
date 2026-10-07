import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { inject, Injector, Service, type Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable, retry } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { lazyFeatureAction } from './lazy-feature-action';

@Service()
class ActionOwner {
    private readonly http = inject(HttpClient);
    public readonly request = vi.fn((): Observable<string> =>
        this.http.post<string>(
            '/action',
            {},
            {
                headers: { 'Idempotency-Key': crypto.randomUUID() },
            },
        ),
    );
}

let injector: Injector;
let http: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    injector = TestBed.inject(Injector);
    http = TestBed.inject(HttpTestingController);
});
afterEach(() => {
    http.verify();
});

async function loadOwnerAsync(): Promise<Type<ActionOwner>> {
    return new Promise<Type<ActionOwner>>(resolve => {
        resolve(ActionOwner);
    });
}

describe('Lazy feature actions', () => {
    it('starts loading only on subscription and resolves the existing owner from DI', async () => {
        const loadAsync = vi.fn(loadOwnerAsync);
        const owner = TestBed.inject(ActionOwner);
        const request = lazyFeatureAction(injector, loadAsync, service => service.request());
        expect(loadAsync).not.toHaveBeenCalled();
        expect(owner.request).not.toHaveBeenCalled();
        const result = firstValueFrom(request);
        await vi.waitFor(() => {
            expect(owner.request).toHaveBeenCalledOnce();
        });
        http.expectOne('/action').flush('saved');
        expect(await result).toBe('saved');
    });

    it('keeps the same idempotency key and cold HTTP request across a retry', async () => {
        const loadAsync = vi.fn(loadOwnerAsync);
        const result = firstValueFrom(lazyFeatureAction(injector, loadAsync, service => service.request()).pipe(retry(1)));
        await vi.waitFor(() => {
            expect(TestBed.inject(ActionOwner).request).toHaveBeenCalledOnce();
        });
        const first = http.expectOne('/action');
        const key = first.request.headers.get('Idempotency-Key');
        first.flush('retry', { status: 503, statusText: 'Unavailable' });
        const second = http.expectOne('/action');
        expect(second.request.headers.get('Idempotency-Key')).toBe(key);
        second.flush('saved');
        expect(await result).toBe('saved');
        expect(loadAsync).toHaveBeenCalledOnce();
        expect(TestBed.inject(ActionOwner).request).toHaveBeenCalledOnce();
    });

    it('does not invoke the owner after cancellation during module loading', async () => {
        let complete: ((value: Type<ActionOwner>) => void) | undefined;
        const loading = new Promise<Type<ActionOwner>>(resolve => {
            complete = resolve;
        });
        const invoke = vi.fn((service: ActionOwner) => service.request());
        const subscription = lazyFeatureAction(injector, async () => loading, invoke).subscribe();
        subscription.unsubscribe();
        complete?.(ActionOwner);
        await loading;
        await new Promise<void>(resolve => {
            setTimeout(resolve, 0);
        });
        expect(invoke).not.toHaveBeenCalled();
        http.expectNone('/action');
    });

    it('cancels an in-flight HTTP request when the consumer unsubscribes', async () => {
        const subscription = lazyFeatureAction(injector, loadOwnerAsync, service => service.request()).subscribe();
        await vi.waitFor(() => {
            expect(TestBed.inject(ActionOwner).request).toHaveBeenCalledOnce();
        });
        const request = http.expectOne('/action');
        subscription.unsubscribe();
        expect(request.cancelled).toBe(true);
    });

    it('propagates a loader failure and allows another subscription to retry loading', async () => {
        const failure = new Error('Chunk unavailable');
        const loadAsync = vi.fn<() => Promise<Type<ActionOwner>>>().mockRejectedValueOnce(failure).mockResolvedValue(ActionOwner);
        const request = lazyFeatureAction(injector, loadAsync, service => service.request());
        await expect(firstValueFrom(request)).rejects.toBe(failure);
        const result = firstValueFrom(request);
        await vi.waitFor(() => {
            expect(TestBed.inject(ActionOwner).request).toHaveBeenCalledOnce();
        });
        http.expectOne('/action').flush('saved');
        expect(await result).toBe('saved');
    });
});
