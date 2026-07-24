import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { SwUpdate, type UnrecoverableStateEvent } from '@angular/service-worker';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AppVersionRecoveryService } from './app-version-recovery.service';

describe('AppVersionRecoveryService', () => {
    let unrecoverable: Subject<UnrecoverableStateEvent>;
    let reload: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        unrecoverable = new Subject<UnrecoverableStateEvent>();
        reload = vi.fn();
        TestBed.configureTestingModule({
            providers: [
                AppVersionRecoveryService,
                {
                    provide: SwUpdate,
                    useValue: {
                        isEnabled: true,
                        unrecoverable,
                    },
                },
                {
                    provide: DOCUMENT,
                    useValue: {
                        defaultView: {
                            location: { reload },
                        },
                    },
                },
            ],
        });
    });

    it('reloads the page when the active service-worker version becomes unrecoverable', () => {
        const service = TestBed.inject(AppVersionRecoveryService);
        service.initialize();

        unrecoverable.next({
            type: 'UNRECOVERABLE_STATE',
            reason: 'A lazy chunk is no longer available.',
        });

        expect(reload).toHaveBeenCalledOnce();
    });

    it('subscribes only once when initialized repeatedly', () => {
        const service = TestBed.inject(AppVersionRecoveryService);
        service.initialize();
        service.initialize();

        unrecoverable.next({
            type: 'UNRECOVERABLE_STATE',
            reason: 'A lazy chunk is no longer available.',
        });

        expect(reload).toHaveBeenCalledOnce();
    });
});
