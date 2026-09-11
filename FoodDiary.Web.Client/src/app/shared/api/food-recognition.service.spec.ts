import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import type { FoodRecognitionJob } from '../models/food-recognition.data';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { FoodRecognitionService } from './food-recognition.service';

const HASH_BYTES = 32;
const POST_RETRY_MS = 1000;
const POLL_INTERVAL_MS = 5000;

const hub = vi.hoisted(() => ({ handlers: new Map<string, (id: string) => void>(), stop: vi.fn().mockResolvedValue(undefined) }));
vi.mock('@microsoft/signalr', () => ({
    LogLevel: { None: 0 },
    HubConnectionBuilder: class {
        public withUrl(): this {
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
                start: vi.fn().mockRejectedValue(new Error('websocket unavailable')),
                stop: hub.stop,
                on: (name: string, handler: (id: string) => void): void => {
                    hub.handlers.set(name, handler);
                },
                onreconnected: vi.fn(),
                onclose: vi.fn(),
            };
        }
    },
}));
const url = `${environment.apiUrls.ai}/food/recognitions`;
const request = { imageAssetId: 'image-1' };
const user = signal<string | null>('user-1');
let service: FoodRecognitionService;
let http: HttpTestingController;
let storage: Map<string, string>;
function job(id: string, status: FoodRecognitionJob['status'] = 'Running'): FoodRecognitionJob {
    return {
        id,
        imageAssetId: request.imageAssetId,
        imageUrl: 'https://example.com/photo.jpg',
        description: null,
        status,
        createdOnUtc: '2026-09-11T00:00:00Z',
        updatedOnUtc: '2026-09-11T00:00:00Z',
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
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: { getUserId: user, getToken: (): string => 'test-token' } },
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
        service.resume(id).subscribe(result);
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${url}/${id}`).flush(job(id, 'Succeeded'));
        await vi.advanceTimersByTimeAsync(0);
        http.expectNone(url);
        expect(result).toHaveBeenCalledOnce();
        expect(storage.size).toBe(0);
    });

    it('uses SignalR only as a hint and fetches the owner-scoped saved result', async () => {
        const result = vi.fn();
        const subscription = service.resume('job-1').subscribe(result);
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
        service.resume('job-1').subscribe({ next: result, error });
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
