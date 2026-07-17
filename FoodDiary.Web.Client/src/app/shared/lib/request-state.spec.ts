import { describe, expect, it } from 'vitest';

import { RequestStateController } from './request-state';

describe('RequestStateController', () => {
    it('models idle, loading, and success states', () => {
        const request = new RequestStateController<{ value: number }>();

        expect(request.state()).toEqual({ status: 'idle', data: null, error: null });
        const requestId = request.begin();
        expect(request.state()).toEqual({ status: 'loading', data: null, error: null });

        expect(request.succeed(requestId, { value: 1 })).toBe(true);
        expect(request.state()).toEqual({ status: 'success', data: { value: 1 }, error: null });
    });

    it('preserves existing data during refresh and refresh failure', () => {
        const request = new RequestStateController<string>();
        request.succeed(request.begin(), 'cached');

        const refreshId = request.begin();
        expect(request.state()).toEqual({ status: 'loading', data: 'cached', error: null });
        request.fail(refreshId, 'LOAD_FAILED');

        expect(request.state()).toEqual({ status: 'error', data: 'cached', error: 'LOAD_FAILED' });
    });

    it('supports silent refresh without exposing a loading state', () => {
        const request = new RequestStateController<string>();
        request.succeed(request.begin(), 'cached');

        const refreshId = request.begin({ showLoading: false });
        expect(request.state()).toEqual({ status: 'success', data: 'cached', error: null });
        request.succeed(refreshId, 'fresh');

        expect(request.data()).toBe('fresh');
    });

    it('ignores stale responses', () => {
        const request = new RequestStateController<string>();
        const staleId = request.begin();
        const currentId = request.begin();

        expect(request.succeed(staleId, 'stale')).toBe(false);
        expect(request.fail(staleId, 'STALE_ERROR')).toBe(false);
        expect(request.succeed(currentId, 'current')).toBe(true);
        expect(request.data()).toBe('current');
    });

    it('invalidates in-flight responses when reset', () => {
        const request = new RequestStateController<string>();
        const requestId = request.begin();

        request.reset();

        expect(request.succeed(requestId, 'late')).toBe(false);
        expect(request.state()).toEqual({ status: 'idle', data: null, error: null });
    });
});
