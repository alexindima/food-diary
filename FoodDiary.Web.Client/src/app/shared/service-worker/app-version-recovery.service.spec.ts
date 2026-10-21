import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SwUpdate, type UnrecoverableStateEvent } from '@angular/service-worker';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AppVersionRecoveryService } from './app-version-recovery.service';
import { ServiceWorkerDiagnosticsService } from './service-worker-diagnostics.service';

describe('AppVersionRecoveryService', () => {
    let unrecoverable: Subject<UnrecoverableStateEvent>;
    let reload: ReturnType<typeof vi.fn>;
    let report: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        unrecoverable = new Subject<UnrecoverableStateEvent>();
        reload = vi.fn();
        report = vi.fn();
        TestBed.configureTestingModule({
            providers: [
                AppVersionRecoveryService,
                { provide: PLATFORM_ID, useValue: 'browser' },
                { provide: ServiceWorkerDiagnosticsService, useValue: { report } },
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

    it('requests explicit recovery without reloading or reporting the raw worker reason', () => {
        const service = TestBed.inject(AppVersionRecoveryService);
        service.initialize();

        unrecoverable.next({
            type: 'UNRECOVERABLE_STATE',
            reason: 'A lazy chunk is no longer available.',
        });

        expect(service.reloadRequired()).toBe(true);
        expect(reload).not.toHaveBeenCalled();
        expect(report).toHaveBeenCalledExactlyOnceWith('recovery');
    });

    it('subscribes only once when initialized repeatedly', () => {
        const service = TestBed.inject(AppVersionRecoveryService);
        service.initialize();
        service.initialize();

        unrecoverable.next({
            type: 'UNRECOVERABLE_STATE',
            reason: 'A lazy chunk is no longer available.',
        });

        expect(service.reloadRequired()).toBe(true);
        expect(report).toHaveBeenCalledOnce();
        expect(reload).not.toHaveBeenCalled();
    });

    it.each(['server', 'disabled'])('does not request recovery in the %s context', context => {
        if (context === 'server') {
            TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
        } else {
            TestBed.overrideProvider(SwUpdate, { useValue: { isEnabled: false } });
        }
        const service = TestBed.inject(AppVersionRecoveryService);
        service.initialize();
        unrecoverable.next({ type: 'UNRECOVERABLE_STATE', reason: 'Ignored' });
        expect(service.reloadRequired()).toBe(false);
        expect(report).not.toHaveBeenCalled();
    });

    it('releases its worker listener when the application is destroyed', () => {
        const service = TestBed.inject(AppVersionRecoveryService);
        service.initialize();
        TestBed.resetTestingModule();
        unrecoverable.next({ type: 'UNRECOVERABLE_STATE', reason: 'Ignored' });
        expect(service.reloadRequired()).toBe(false);
        expect(report).not.toHaveBeenCalled();
    });
});
