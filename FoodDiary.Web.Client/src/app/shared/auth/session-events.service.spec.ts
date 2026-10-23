import { firstValueFrom, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { SessionEventsService } from './session-events.service';

describe('SessionEventsService session end cleanup', () => {
    it('bounds the guard bypass to the awaited session departure, including a rejected navigation', async () => {
        const service = new SessionEventsService();
        const navigation = new Subject<void>();
        const pending = service.runSessionEndNavigationAsync(async () => {
            await firstValueFrom(navigation);
        });
        expect(service.isSessionEndNavigation).toBe(true);
        navigation.next();
        await pending;
        expect(service.isSessionEndNavigation).toBe(false);

        await expect(
            service.runSessionEndNavigationAsync(
                async () =>
                    new Promise<void>((_resolve, reject) => {
                        reject(new Error('navigation failed'));
                    }),
            ),
        ).rejects.toThrow('navigation failed');
        expect(service.isSessionEndNavigation).toBe(false);
    });
    it('invalidates local consumers without running shared-session cleanup', () => {
        const service = new SessionEventsService();
        const cleanup = vi.fn().mockResolvedValue(undefined);
        const ended = vi.fn();
        service.registerSessionEndCleanup(cleanup);
        service.sessionEnded$.subscribe(ended);

        service.notifyLocalSessionInvalidated();

        expect(service.isLocalSessionInvalidated).toBe(true);
        expect(ended).toHaveBeenCalledOnce();
        expect(cleanup).not.toHaveBeenCalled();
        service.notifyAuthenticated();
        expect(service.isLocalSessionInvalidated).toBe(false);
    });
    it('returns null without registered cleanup so logout can remain synchronous', () => {
        const service = new SessionEventsService();

        expect(service.prepareSessionEnd()).toBeNull();
    });

    it('starts every cleanup synchronously and waits for all of them', async () => {
        const service = new SessionEventsService();
        const first = new Subject<void>();
        const second = new Subject<void>();
        let firstFinished = false;
        const firstCleanup = vi.fn(async () => {
            await firstValueFrom(first);
            firstFinished = true;
        });
        const secondCleanup = vi.fn(async () => {
            await firstValueFrom(second);
        });
        service.registerSessionEndCleanup(firstCleanup);
        service.registerSessionEndCleanup(secondCleanup);

        const pending = service.prepareSessionEnd();
        const finished = vi.fn();
        void pending?.then(finished);

        expect(firstCleanup).toHaveBeenCalledOnce();
        expect(secondCleanup).toHaveBeenCalledOnce();
        first.next();
        await vi.waitFor(() => {
            expect(firstFinished).toBe(true);
        });
        expect(finished).not.toHaveBeenCalled();

        second.next();
        await pending;

        expect(finished).toHaveBeenCalledOnce();
    });
});

describe('SessionEventsService cleanup completion', () => {
    it('settles thrown and rejected cleanup without skipping another cleanup', async () => {
        const service = new SessionEventsService();
        const later = vi.fn().mockResolvedValue(undefined);
        service.registerSessionEndCleanup(() => {
            throw new Error('synchronous cleanup failure');
        });
        service.registerSessionEndCleanup(vi.fn().mockRejectedValue(new Error('asynchronous cleanup failure')));
        service.registerSessionEndCleanup(later);

        await expect(service.prepareSessionEnd()).resolves.toBeUndefined();

        expect(later).toHaveBeenCalledOnce();
    });

    it('unregisters cleanup without affecting other registered owners', async () => {
        const service = new SessionEventsService();
        const removed = vi.fn().mockResolvedValue(undefined);
        const retained = vi.fn().mockResolvedValue(undefined);
        const unregister = service.registerSessionEndCleanup(removed);
        const unregisterRetained = service.registerSessionEndCleanup(retained);

        unregister();
        unregister();
        await service.prepareSessionEnd();

        expect(removed).not.toHaveBeenCalled();
        expect(retained).toHaveBeenCalledOnce();
        unregisterRetained();
        expect(service.prepareSessionEnd()).toBeNull();
    });

    it('keeps authentication and ended events separate from preparation', async () => {
        const service = new SessionEventsService();
        const authenticated = vi.fn();
        const ended = vi.fn();
        service.authenticated$.subscribe(authenticated);
        service.sessionEnded$.subscribe(ended);
        service.registerSessionEndCleanup(vi.fn().mockResolvedValue(undefined));

        await service.prepareSessionEnd();

        expect(authenticated).not.toHaveBeenCalled();
        expect(ended).not.toHaveBeenCalled();
        service.notifyAuthenticated();
        service.notifySessionEnded();
        expect(authenticated).toHaveBeenCalledOnce();
        expect(ended).toHaveBeenCalledOnce();
    });
});
