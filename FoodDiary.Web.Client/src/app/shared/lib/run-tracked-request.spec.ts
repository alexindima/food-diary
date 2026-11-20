import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { createEnvironmentInjector, DestroyRef, EnvironmentInjector, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { runTrackedRequest } from './run-tracked-request';

function createDestroyRef(): DestroyRef {
    return { onDestroy: vi.fn(() => vi.fn()) } as unknown as DestroyRef;
}

describe('runTrackedRequest', () => {
    it('sets state around successful request and forwards value', () => {
        const state = signal(false);
        const next = vi.fn();

        runTrackedRequest(createDestroyRef(), state, of('done'), { next });

        expect(next).toHaveBeenCalledWith('done');
        expect(state()).toBe(false);
    });

    it('sets state back to false after request error', () => {
        const state = signal(false);
        const error = new Error('Boom');
        const errorHandler = vi.fn();

        runTrackedRequest(
            createDestroyRef(),
            state,
            throwError(() => error),
            { error: errorHandler },
        );

        expect(errorHandler).toHaveBeenCalledWith(error);
        expect(state()).toBe(false);
    });

    it('cancels an in-flight HTTP mutation and clears loading when its owner is destroyed', () => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        const owner = createEnvironmentInjector([], TestBed.inject(EnvironmentInjector));
        const http = owner.get(HttpClient);
        const requests = TestBed.inject(HttpTestingController);
        const path = '/api/v1/hydrations';
        const entry = { amountMl: 250, timestampUtc: '2026-11-20T09:00:00.000Z' };
        const state = signal(false);
        const next = vi.fn();
        const error = vi.fn();
        try {
            runTrackedRequest(owner.get(DestroyRef), state, http.post(path, entry), { next, error });
            const pending = requests.expectOne({ method: 'POST', url: path });
            expect(state()).toBe(true);
            expect(pending.cancelled).toBe(false);

            owner.destroy();

            expect(pending.cancelled).toBe(true);
            expect(state()).toBe(false);
            expect(next).not.toHaveBeenCalled();
            expect(error).not.toHaveBeenCalled();
        } finally {
            if (!owner.destroyed) {
                owner.destroy();
            }
            requests.verify();
        }
    });
});
