import type { DestroyRef } from '@angular/core';
import { signal } from '@angular/core';
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
});
