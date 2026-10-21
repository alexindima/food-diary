import { isPlatformBrowser } from '@angular/common';
import { DestroyRef, inject, PLATFORM_ID, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwUpdate } from '@angular/service-worker';

import { ServiceWorkerDiagnosticsService } from './service-worker-diagnostics.service';

@Service()
export class AppVersionRecoveryService {
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly destroyRef = inject(DestroyRef);
    private readonly swUpdate = inject(SwUpdate, { optional: true });
    private readonly diagnostics = inject(ServiceWorkerDiagnosticsService);
    private readonly recoveryRequired = signal(false);
    private initialized = false;

    public readonly reloadRequired = this.recoveryRequired.asReadonly();

    public initialize(): void {
        if (this.initialized || !this.isBrowser || this.swUpdate?.isEnabled !== true) {
            return;
        }

        this.initialized = true;
        this.swUpdate.unrecoverable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.recoveryRequired.set(true);
            this.diagnostics.report('recovery');
        });
    }
}
