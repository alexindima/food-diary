import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import type { FoodVisionResponse } from '../models/ai.data';
import type { FoodRecognitionJob } from '../models/food-recognition.data';
import { utcInstant } from '../models/semantics/date-value';
import { entityId } from '../models/semantics/entity-id';
import { publicImageUrl } from '../models/semantics/image-location';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { FoodRecognitionService } from './food-recognition.service';
import type { FoodRecognitionJobHttpResponse } from './sdk/generated/model/food-recognition-job-http-response';

const HASH_BYTES = 32;
const POST_RETRY_MS = 1000;
const POLL_INTERVAL_MS = 5000;

const hub = vi.hoisted(() => ({
    handlers: new Map<string, (id: string) => void>(),
    url: vi.fn<(url: string, options: { accessTokenFactory: () => string; headers: Record<string, string> }) => void>(),
    start: vi.fn<() => Promise<void>>(),
    stop: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
    onreconnected: vi.fn<(handler: () => void) => void>(),
    onclose: vi.fn<(handler: () => void) => void>(),
}));
vi.mock('@microsoft/signalr', () => ({
    LogLevel: { None: 0 },
    HubConnectionBuilder: class {
        public withUrl(url: string, options: { accessTokenFactory: () => string; headers: Record<string, string> }): this {
            hub.url(url, options);
            return this;
        }
        public withAutomaticReconnect(): this {
            return this;
        }
        public configureLogging(): this {
            return this;
        }
        public build(): object {
            return {
                start: hub.start,
                stop: hub.stop,
                on: (name: string, handler: (id: string) => void): void => {
                    hub.handlers.set(name, handler);
                },
                onreconnected: hub.onreconnected,
                onclose: hub.onclose,
            };
        }
    },
}));
const url = `${environment.apiUrls.ai}/food/recognitions`;
const request = { imageAssetId: entityId<'image-asset'>('image-1') };
const user = signal<string | null>('user-1');
const getToken = vi.fn<() => string | null>();
let service: FoodRecognitionService;
let http: HttpTestingController;
let storage: Map<string, string>;
function job(id: string, status: FoodRecognitionJob['status'] = 'Running'): FoodRecognitionJob {
    return {
        id: entityId<'food-recognition'>(id),
        imageAssetId: entityId<'image-asset'>(request.imageAssetId),
        imageUrl: publicImageUrl('https://example.com/photo.jpg'),
        description: null,
        status,
        createdOnUtc: utcInstant('2026-09-11T00:00:00Z'),
        updatedOnUtc: utcInstant('2026-09-11T00:00:00Z'),
        vision: status === 'Succeeded' ? { items: [], notes: null } : null,
        nutrition: null,
        errorCode: null,
        nutritionErrorCode: null,
    };
}
beforeEach(() => {
    vi.useFakeTimers();
    vi.spyOn(crypto.subtle, 'digest').mockResolvedValue(new Uint8Array(HASH_BYTES).buffer);
    storage = new Map();
    user.set('user-1');
    hub.handlers.clear();
    hub.url.mockClear();
    getToken.mockReset().mockReturnValue('test-token');
    hub.start.mockReset().mockRejectedValue(new Error('websocket unavailable'));
    hub.stop.mockClear();
    hub.onreconnected.mockClear();
    hub.onclose.mockClear();
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            {
                provide: AuthService,
                useValue: { getUserId: user, getToken, sessionHubUrl: (hubUrl: string): string => hubUrl },
            },
            {
                provide: BrowserStorageService,
                useValue: {
                    getItem: (_scope: string, key: string): string | null => storage.get(key) ?? null,
                    setItem: (_scope: string, key: string, value: string): void => {
                        storage.set(key, value);
                    },
                    removeItem: (_scope: string, key: string): void => {
                        storage.delete(key);
                    },
                },
            },
        ],
    });
    service = TestBed.inject(FoodRecognitionService);
    http = TestBed.inject(HttpTestingController);
    TestBed.tick();
});
afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
    vi.restoreAllMocks();
    vi.useRealTimers();
});

describe('durable food recognition', () => {
    it('includes product mode and every photo in the request fingerprint and releases the completed request', async () => {
        const digest = vi.spyOn(crypto.subtle, 'digest');
        const productRequest = {
            ...request,
            isProductLabel: true,
            additionalImageAssetIds: [entityId<'image-asset'>('image-2'), entityId<'image-asset'>('image-3')],
        };
        const result = vi.fn();
        service.start(productRequest).subscribe(result);
        await vi.advanceTimersByTimeAsync(0);
        const accepted = http.expectOne(url);
        const id = (accepted.request.body as { id: string }).id;
        expect(accepted.request.body).toEqual({ id, ...productRequest });
        const completed: FoodRecognitionJob = {
            ...job(id, 'Succeeded'),
            isProductLabel: true,
            additionalImages: productRequest.additionalImageAssetIds.map(imageAssetId => ({
                imageAssetId,
                imageUrl: publicImageUrl('https://example.com/label.jpg'),
            })),
        };
        accepted.flush(completed);
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${id}`).flush(completed);
        await vi.advanceTimersByTimeAsync(0);
        const fingerprints = digest.mock.calls.map(call => new TextDecoder().decode(call[1] as ArrayBuffer));
        expect(fingerprints.length).toBeGreaterThanOrEqual(2);
        expect(fingerprints.map(value => JSON.parse(value) as object)).toEqual(
            fingerprints.map(() => ({ ...productRequest, description: null })),
        );
        expect(result).toHaveBeenCalledOnce();
        expect(storage.size).toBe(0);
    });

    it('reuses the task id after a lost acceptance response and completes with HTTP when SignalR is unavailable', async () => {
        const result = vi.fn();
        service.start(request).subscribe(result);
        await vi.advanceTimersByTimeAsync(0);
        const first = http.expectOne(url);
        const id = (first.request.body as { id: string }).id;
        first.error(new ProgressEvent('offline'));
        await vi.advanceTimersByTimeAsync(POST_RETRY_MS);
        const retry = http.expectOne(url);
        expect(retry.request.body).toEqual({ id, ...request });
        retry.flush(job(id));
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${id}`).error(new ProgressEvent('offline'));
        await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);
        http.expectOne(`${url}/${id}`).flush(job(id, 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);
        expect(result).toHaveBeenCalledOnce();
        expect(storage.size).toBe(0);
    });

    it('keeps the task id after closing the view and resumes without submitting another paid operation', async () => {
        const waiting = service.start(request).subscribe();
        await vi.advanceTimersByTimeAsync(0);
        const accepted = http.expectOne(url);
        const id = (accepted.request.body as { id: string }).id;
        accepted.flush(job(id));
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${id}`).flush(job(id));
        waiting.unsubscribe();
        expect(storage.size).toBe(1);
        const result = vi.fn();
        service.resume(entityId<'food-recognition'>(id)).subscribe(result);
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${id}`).flush(job(id, 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);
        http.expectNone(url);
        expect(result).toHaveBeenCalledOnce();
        expect(storage.size).toBe(0);
    });
});

describe('recognition notifications', () => {
    it('uses SignalR only as a hint and fetches the owner-scoped saved result', async () => {
        const result = vi.fn();
        const subscription = service.resume(entityId<'food-recognition'>('job-1')).subscribe(result);
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/job-1`).flush(job('job-1'));
        hub.handlers.get('RecognitionChanged')?.('another-job');
        http.expectNone(`${url}/job-1`);
        hub.handlers.get('RecognitionChanged')?.('job-1');
        http.expectOne(`${url}/job-1`).flush(job('job-1', 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);
        expect(result).toHaveBeenCalledOnce();
        subscription.unsubscribe();
    });
});

describe('recognition terminal states', () => {
    it('reports terminal failure without automatic restart and permits an explicit fresh attempt', async () => {
        const error = vi.fn();
        service.start(request).subscribe({ error });
        await vi.advanceTimersByTimeAsync(0);
        const accepted = http.expectOne(url);
        const id = (accepted.request.body as { id: string }).id;
        accepted.flush(job(id));
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${id}`).flush({ ...job(id, 'Failed'), errorCode: 'Ai.RecognitionInterrupted' });
        await vi.advanceTimersByTimeAsync(0);
        expect(error).toHaveBeenCalledWith(expect.any(HttpErrorResponse));
        expect(storage.size).toBe(0);
        const next = service.start(request).subscribe();
        await vi.advanceTimersByTimeAsync(0);
        const restarted = http.expectOne(url);
        expect((restarted.request.body as { id: string }).id).not.toBe(id);
        next.unsubscribe();
    });

    it('does not display an in-flight response after the signed-in user changes', async () => {
        const result = vi.fn();
        const error = vi.fn();
        service.resume(entityId<'food-recognition'>('job-1')).subscribe({ next: result, error });
        await vi.advanceTimersByTimeAsync(0);
        const pending = http.expectOne(`${url}/job-1`);
        user.set('user-2');
        TestBed.tick();
        pending.flush(job('job-1', 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);
        expect(result).not.toHaveBeenCalled();
        expect(error).toHaveBeenCalledWith(expect.objectContaining({ status: 401 }));
    });
});

it('deletes only the requested recognition endpoint', () => {
    const done = vi.fn();
    service.deleteRecognition(entityId<'food-recognition'>('completed-id')).subscribe(done);
    const deletion = http.expectOne(`${url}/completed-id`);
    expect(deletion.request.method).toBe('DELETE');
    deletion.flush(null);
    expect(done).toHaveBeenCalledTimes(1);
});

it('requests a server-filtered page and preserves total count', () => {
    const received = vi.fn();
    service.list(2, undefined, true).subscribe(received);
    const pageRequest = http.expectOne(item => item.url === `${url}/page` && item.method === 'GET');
    expect(pageRequest.request.params.get('page')).toBe('2');
    expect(pageRequest.request.params.get('limit')).toBe('20');
    expect(pageRequest.request.params.get('isProductLabel')).toBe('true');
    const page = { data: [job('page-2', 'Succeeded')], page: 2, limit: 20, totalPages: 2, totalItems: 21 };
    pageRequest.flush(page);
    expect(received).toHaveBeenCalledWith(page);
});

describe('saved product label recognition', () => {
    it('keeps an explicit zero separate from unknown nutrition and preserves the photographed label’s basis', async () => {
        const pending = firstValueFrom(service.list(1, undefined, true));
        const baseAmount = 100;
        const savedLabel = { name: 'Sparkling water', baseAmount, baseUnit: 'ml', calories: 0, carbs: 0 };
        const saved: FoodRecognitionJobHttpResponse = {
            ...job('water-label', 'Succeeded'),
            isProductLabel: true,
            vision: { items: [], productLabel: savedLabel },
        };
        const page = http.expectOne(item => item.url === `${url}/page` && item.method === 'GET');
        expect(page.request.params.get('isProductLabel')).toBe('true');
        page.flush({ data: [saved], page: 1, limit: 20, totalPages: 1, totalItems: 1 });

        const result = await pending;
        expect(result.data).toHaveLength(1);
        expect(result.data[0].vision?.productLabel).toEqual({
            ...savedLabel,
            brand: null,
            protein: null,
            fat: null,
            fiber: null,
            alcohol: null,
            notes: null,
        });
    });

    it('rejects an unsupported saved label unit before exposing the page for import', async () => {
        const pending = firstValueFrom(service.list(1, undefined, true));
        const saved: FoodRecognitionJobHttpResponse = {
            ...job('unsupported-basis', 'Succeeded'),
            isProductLabel: true,
            vision: { items: [], productLabel: { name: 'Dry beans', baseAmount: 1, baseUnit: 'kg' } },
        };
        http.expectOne(item => item.url === `${url}/page`).flush({ data: [saved], page: 1, limit: 20, totalPages: 1, totalItems: 1 });

        await expect(pending).rejects.toThrow('unsupported enum value');
    });
});

describe('recognition request ownership', () => {
    it('rejects start and resume as a guest without creating a request', () => {
        user.set(null);
        TestBed.tick();
        const onStartError = vi.fn();
        const onResumeError = vi.fn();

        service.start(request).subscribe({ error: onStartError });
        service.resume(entityId<'food-recognition'>('guest-job')).subscribe({ error: onResumeError });

        expect(onStartError).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));
        expect(onResumeError).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));
        http.expectNone(url);
        http.expectNone(`${url}/guest-job`);
        expect(storage.size).toBe(0);
    });

    it('does not submit photos for a former account when fingerprint creation finishes late', async () => {
        let finishHash: (value: ArrayBuffer) => void = () => {};
        vi.spyOn(crypto.subtle, 'digest').mockReturnValueOnce(
            new Promise<ArrayBuffer>(resolve => {
                finishHash = resolve;
            }),
        );
        const onError = vi.fn();
        const waiting = service.start(request).subscribe({ error: onError });

        user.set('user-2');
        TestBed.tick();
        finishHash(new Uint8Array(HASH_BYTES).buffer);
        await vi.advanceTimersByTimeAsync(0);
        const oldPosts = http.match({ method: 'POST', url });
        waiting.unsubscribe();

        expect(oldPosts).toHaveLength(0);
        expect(onError).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));
        expect(storage.size).toBe(0);
    });

    it('stops an automatic POST retry after an account change and retains the saved task token for its owner', async () => {
        const onError = vi.fn();
        const waiting = service.start(request).subscribe({ error: onError });
        await vi.advanceTimersByTimeAsync(0);
        const first = http.expectOne({ method: 'POST', url });
        const taskId = (first.request.body as { id: string }).id;
        first.error(new ProgressEvent('offline'));

        user.set('user-2');
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(POST_RETRY_MS);
        const oldPosts = http.match({ method: 'POST', url });
        waiting.unsubscribe();

        expect(oldPosts).toHaveLength(0);
        expect(onError).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));
        expect([...storage.values()]).toEqual([taskId]);

        user.set('user-1');
        TestBed.tick();
        const revisited = service.start(request).subscribe();
        await vi.advanceTimersByTimeAsync(0);
        const retry = http.expectOne({ method: 'POST', url });
        expect(retry.request.body).toEqual({ id: taskId, ...request });
        revisited.unsubscribe();
    });
});

describe('recognition service rejection handling', () => {
    it('does not retry validation failure and removes its saved request token', async () => {
        const onError = vi.fn();
        service.start(request).subscribe({ error: onError });
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne({ method: 'POST', url }).flush(
            { error: 'Validation.Invalid' },
            { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' },
        );
        await vi.advanceTimersByTimeAsync(POST_RETRY_MS);

        http.expectNone(url);
        expect(onError).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.BadRequest }));
        expect(storage.size).toBe(0);
    });

    it.each([
        ['Ai.QuotaExceeded', HttpStatusCode.TooManyRequests],
        ['Ai.TrialEnded', HttpStatusCode.Forbidden],
        ['Ai.ConsentRequired', HttpStatusCode.Forbidden],
    ])('reports terminal %s without opening a new recognition', async (code, status) => {
        const onError = vi.fn();
        service.resume(entityId<'food-recognition'>('saved-job')).subscribe({ error: onError });
        await vi.advanceTimersByTimeAsync(0);
        const endpoint = `${url}/saved-job`;
        http.expectOne(endpoint).flush({ ...job('saved-job', 'Failed'), errorCode: code });
        await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);

        expect(onError).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                status,
                error: { code, terminal: true },
            }),
        );
        http.expectNone(endpoint);
        http.expectNone(url);
    });
});

describe('recognition hub lifecycle', () => {
    it('shares a connection for two active jobs and checks both after reconnection', async () => {
        hub.start.mockResolvedValue(undefined);
        const first = vi.fn<(value: FoodVisionResponse) => void>();
        const second = vi.fn<(value: FoodVisionResponse) => void>();
        service.resume(entityId<'food-recognition'>('first-job')).subscribe(first);
        service.resume(entityId<'food-recognition'>('second-job')).subscribe(second);
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/first-job`).flush(job('first-job'));
        http.expectOne(`${url}/second-job`).flush(job('second-job'));
        expect(hub.start).toHaveBeenCalledOnce();

        const reconnected = hub.onreconnected.mock.calls[0][0];
        reconnected();
        http.expectOne(`${url}/first-job`).flush(job('first-job', 'Succeeded'));
        http.expectOne(`${url}/second-job`).flush(job('second-job', 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);

        expect(first).toHaveBeenCalledOnce();
        expect(second).toHaveBeenCalledOnce();
        expect(first.mock.calls[0][0].recognition?.id).toBe('first-job');
        expect(second.mock.calls[0][0].recognition?.id).toBe('second-job');
    });

    it('stops the old connection once and ignores its late close after another account connects', async () => {
        hub.start.mockResolvedValue(undefined);
        const error = vi.fn();
        service.resume(entityId<'food-recognition'>('old-job')).subscribe({ error });
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/old-job`).flush(job('old-job'));
        const oldClosed = hub.onclose.mock.calls[0][0];

        user.set('user-2');
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        expect(hub.stop).toHaveBeenCalledOnce();
        expect(error).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));

        const newJob = service.resume(entityId<'food-recognition'>('new-job')).subscribe();
        await vi.advanceTimersByTimeAsync(0);
        const joined = http.expectOne(`${url}/new-job`);
        oldClosed();
        const otherJob = service.resume(entityId<'food-recognition'>('other-job')).subscribe();
        await vi.advanceTimersByTimeAsync(0);
        const other = http.expectOne(`${url}/other-job`);

        expect(hub.start).toHaveBeenCalledTimes(2);
        expect(joined.cancelled).toBe(false);
        expect(other.cancelled).toBe(false);
        newJob.unsubscribe();
        otherJob.unsubscribe();
    });

    it('uses browser cookie transport and reconnects on the next job after the current hub closes', async () => {
        hub.start.mockResolvedValue(undefined);
        getToken.mockReturnValue(null);
        const first = service.resume(entityId<'food-recognition'>('cookie-job')).subscribe();
        await vi.advanceTimersByTimeAsync(0);
        const firstRead = http.expectOne(`${url}/cookie-job`);
        expect(hub.url).toHaveBeenCalledOnce();
        const [hubUrl, options] = hub.url.mock.calls[0];
        expect(hubUrl).toContain('/hubs/food-recognition');
        expect(options.accessTokenFactory()).toBe('');
        expect(options.headers).toEqual({ 'X-Requested-With': 'XMLHttpRequest' });
        first.unsubscribe();
        expect(firstRead.cancelled).toBe(true);

        const closed = hub.onclose.mock.calls[0][0];
        closed();
        const next = service.resume(entityId<'food-recognition'>('next-job')).subscribe();
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/next-job`);
        expect(hub.start).toHaveBeenCalledTimes(2);
        next.unsubscribe();
    });
});

describe('recognition request recovery across browser and API changes', () => {
    it('stops polling a deleted task and opens an available saved result without resubmitting photos', async () => {
        const missingId = entityId<'food-recognition'>('e65dea27-c63b-419b-927a-519a7ebb58cb');
        const missingUrl = `${url}/${missingId}`;
        const next = vi.fn<(value: FoodVisionResponse) => void>();
        const error = vi.fn<(failure: unknown) => void>();
        const missing = service.resume(missingId).subscribe({ next, error });
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne({ method: 'GET', url: missingUrl }).flush(
            { error: 'Ai.RecognitionNotFound' },
            { status: HttpStatusCode.NotFound, statusText: 'Not Found' },
        );
        await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);

        expect(error).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ status: HttpStatusCode.NotFound, error: { error: 'Ai.RecognitionNotFound' } }),
        );
        expect(missing.closed).toBe(true);
        expect(next).not.toHaveBeenCalled();
        http.expectNone(missingUrl);

        const availableId = entityId<'food-recognition'>('fa676f6f-16d1-4a21-8181-4b64782ee41c');
        service.resume(availableId).subscribe({ next, error });
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne({ method: 'GET', url: `${url}/${availableId}` }).flush(job(availableId, 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);

        expect(next).toHaveBeenCalledOnce();
        expect(next.mock.calls[0][0].recognition?.id).toBe(availableId);
        expect(error).toHaveBeenCalledOnce();
        http.expectNone({ method: 'POST', url });
    });

    it('clears the durable single-label request when the server returns an empty extra-photo list', async () => {
        const digest = vi.spyOn(crypto.subtle, 'digest');
        const productRequest = { ...request, isProductLabel: true };
        const result = vi.fn<(value: FoodVisionResponse) => void>();
        service.start(productRequest).subscribe(result);
        await vi.advanceTimersByTimeAsync(0);
        const submitted = http.expectOne({ method: 'POST', url });
        const id = (submitted.request.body as { id: string }).id;
        expect(submitted.request.body).toEqual({ id, ...productRequest });
        expect([...storage.values()]).toEqual([id]);

        const saved = { ...job(id, 'Succeeded'), isProductLabel: true, additionalImages: [] } satisfies FoodRecognitionJobHttpResponse;
        submitted.flush(saved);
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne({ method: 'GET', url: `${url}/${id}` }).flush(saved);
        await vi.advanceTimersByTimeAsync(0);

        const fingerprints = digest.mock.calls.map(([, bytes]) => new TextDecoder().decode(bytes));
        const normalized = JSON.stringify({ ...request, description: null, isProductLabel: true, additionalImageAssetIds: [] });
        expect(fingerprints).toEqual([normalized, normalized]);
        expect(result).toHaveBeenCalledOnce();
        expect(storage.size).toBe(0);
    });
});

describe('recognition hub ownership during connection startup', () => {
    it('does not start an obsolete hub when the account changes before SignalR can load', async () => {
        hub.start.mockResolvedValue(undefined);
        const old = entityId<'food-recognition'>('pending-old-account');
        const cancelled = expect(firstValueFrom(service.resume(old))).rejects.toMatchObject({ status: HttpStatusCode.Unauthorized });

        user.set('user-2');
        TestBed.tick();
        await cancelled;
        await vi.dynamicImportSettled();
        http.expectNone(`${url}/${old}`);
        expect(hub.url).not.toHaveBeenCalled();

        const active = entityId<'food-recognition'>('current-account');
        const result = firstValueFrom(service.resume(active));
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne({ method: 'GET', url: `${url}/${active}` }).flush(job(active, 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);

        await expect(result).resolves.toMatchObject({ recognition: { id: active } });
        expect(hub.start).toHaveBeenCalledOnce();
        expect(hub.stop).not.toHaveBeenCalled();
        http.expectNone({ method: 'POST', url });
    });

    it('stops a hub that finishes connecting after the active account has changed', async () => {
        const connected = new Subject<void>();
        const connectionStart = firstValueFrom(connected);
        hub.start.mockReturnValueOnce(connectionStart).mockResolvedValue(undefined);
        const original = entityId<'food-recognition'>('before-account-change');
        const originalResult = vi.fn<(value: FoodVisionResponse) => void>();
        const originalError = vi.fn<(failure: unknown) => void>();
        service.resume(original).subscribe({ next: originalResult, error: originalError });
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${original}`).flush(job(original));
        expect(connected.observed).toBe(true);

        connected.next();
        user.set('user-2');
        await connectionStart;
        expect(hub.stop).toHaveBeenCalledOnce();

        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        expect(originalError).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));
        expect(originalResult).not.toHaveBeenCalled();
        http.expectNone(`${url}/${original}`);

        const active = entityId<'food-recognition'>('after-account-change');
        const activeResult = vi.fn<(value: FoodVisionResponse) => void>();
        service.resume(active).subscribe(activeResult);
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${active}`).flush(job(active, 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);

        expect(hub.start).toHaveBeenCalledTimes(2);
        expect(activeResult).toHaveBeenCalledOnce();
        expect(activeResult.mock.calls[0][0].recognition?.id).toBe(active);
    });
});

describe('recognition hub across account switches', () => {
    it('connects the new account when it resumes a job before the previous SignalR import settles', async () => {
        hub.start.mockResolvedValue(undefined);
        const old = entityId<'food-recognition'>('older-account-load');
        const cancelled = expect(firstValueFrom(service.resume(old))).rejects.toMatchObject({ status: HttpStatusCode.Unauthorized });

        user.set('user-2');
        TestBed.tick();
        const active = entityId<'food-recognition'>('active-account-load');
        const endpoint = `${url}/${active}`;
        const result = firstValueFrom(service.resume(active));
        await cancelled;
        await vi.advanceTimersByTimeAsync(0);
        http.expectNone(`${url}/${old}`);
        http.expectOne({ method: 'GET', url: endpoint }).flush(job(active));

        expect(hub.start).toHaveBeenCalledOnce();
        hub.handlers.get('RecognitionChanged')?.(active);
        http.expectOne({ method: 'GET', url: endpoint }).flush(job(active, 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);

        await expect(result).resolves.toMatchObject({ recognition: { id: active } });
        expect(hub.stop).not.toHaveBeenCalled();
        http.expectNone({ method: 'POST', url });
    });

    it('keeps the current hub after the previous account’s connection start rejects', async () => {
        const pendingStart = new Subject<void>();
        hub.start.mockReturnValueOnce(firstValueFrom(pendingStart)).mockResolvedValue(undefined);
        const old = entityId<'food-recognition'>('previous-start');
        const cancelled = expect(firstValueFrom(service.resume(old))).rejects.toMatchObject({ status: HttpStatusCode.Unauthorized });
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${old}`).flush(job(old));
        expect(hub.start).toHaveBeenCalledOnce();

        user.set('user-2');
        TestBed.tick();
        const active = entityId<'food-recognition'>('active-start');
        const activeEndpoint = `${url}/${active}`;
        const first = firstValueFrom(service.resume(active));
        await cancelled;
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(activeEndpoint).flush(job(active));
        expect(hub.start).toHaveBeenCalledTimes(2);

        pendingStart.error(new Error('Previous hub start failed'));
        await vi.advanceTimersByTimeAsync(0);
        const additional = entityId<'food-recognition'>('second-active-job');
        const second = firstValueFrom(service.resume(additional));
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${additional}`).flush(job(additional, 'Succeeded'));
        expect(hub.start).toHaveBeenCalledTimes(2);

        hub.handlers.get('RecognitionChanged')?.(active);
        http.expectOne(activeEndpoint).flush(job(active, 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);
        await expect(first).resolves.toMatchObject({ recognition: { id: active } });
        await expect(second).resolves.toMatchObject({ recognition: { id: additional } });
        expect(hub.stop).toHaveBeenCalledOnce();
    });
});

it('stops polling an invalid saved recognition response and retries the same job on request', async () => {
    const id = entityId<'food-recognition'>('saved-recognition');
    const endpoint = `${url}/${id}`;
    const failed = expect(firstValueFrom(service.resume(id))).rejects.toThrow('API response is missing required field: imageAssetId.');
    await vi.advanceTimersByTimeAsync(0);
    http.expectOne({ method: 'GET', url: endpoint }).flush({ ...job(id, 'Succeeded'), imageAssetId: null });
    await failed;
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);
    http.expectNone(endpoint);

    const reopened = firstValueFrom(service.resume(id));
    await vi.advanceTimersByTimeAsync(0);
    http.expectOne({ method: 'GET', url: endpoint }).flush(job(id, 'Succeeded'));
    await vi.advanceTimersByTimeAsync(0);

    await expect(reopened).resolves.toMatchObject({ recognition: { id } });
    http.expectNone({ method: 'POST', url });
});
