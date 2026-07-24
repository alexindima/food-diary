import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject, Service } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwUpdate } from '@angular/service-worker';

@Service()
export class AppVersionRecoveryService {
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);
    private readonly swUpdate = inject(SwUpdate, { optional: true });
    private initialized = false;

    public initialize(): void {
        if (this.initialized || this.swUpdate?.isEnabled !== true) {
            return;
        }

        this.initialized = true;
        this.swUpdate.unrecoverable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.document.defaultView?.location.reload();
        });
    }
}
