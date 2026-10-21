import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ApplicationRef, computed, DestroyRef, inject, PLATFORM_ID, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwUpdate } from '@angular/service-worker';
import { filter, fromEvent, interval, merge, of, switchMap, take } from 'rxjs';

import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import { AppVersionRecoveryService } from './app-version-recovery.service';
import { ServiceWorkerDiagnosticsService } from './service-worker-diagnostics.service';

const UPDATE_CHECK_INTERVAL_MS = 3_600_000;
const RESUME_CHECK_INTERVAL_MS = 300_000;

@Service()
export class AppUpdateService {
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly appRef = inject(ApplicationRef);
    private readonly swUpdate = inject(SwUpdate, { optional: true });
    private readonly destroyRef = inject(DestroyRef);
    private readonly unsavedChanges = inject(UnsavedChangesService);
    private readonly recovery = inject(AppVersionRecoveryService);
    private readonly diagnostics = inject(ServiceWorkerDiagnosticsService);
    private readonly readyVersion = signal<string | null>(null);
    private readonly dismissedVersion = signal<string | null>(null);
    private initialized = false;
    private checking = false;
    private lastCheckAt: number | null = null;

    public readonly needsSave = signal(false);
    public readonly isReloading = signal(false);
    public readonly recoveryRequired = this.recovery.reloadRequired;
    public readonly hasPendingUpdate = computed(() => this.recoveryRequired() || this.readyVersion() !== null);
    public readonly updateAvailable = computed(
        () => this.recoveryRequired() || (this.readyVersion() !== null && this.readyVersion() !== this.dismissedVersion()),
    );

    public initialize(): void {
        if (this.initialized || !this.isBrowser) {
            return;
        }
        const view = this.document.defaultView;
        if (view === null || this.swUpdate?.isEnabled !== true) {
            return;
        }
        this.initialized = true;
        this.recovery.initialize();
        this.swUpdate.versionUpdates.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            if (event.type === 'VERSION_READY') {
                this.readyVersion.set(event.latestVersion.hash);
                this.needsSave.set(false);
            } else if (event.type === 'VERSION_INSTALLATION_FAILED') {
                this.diagnostics.report('installation', event.version.hash);
            }
        });

        this.appRef.isStable
            .pipe(
                filter(Boolean),
                take(1),
                switchMap(() =>
                    merge(
                        of(null),
                        interval(UPDATE_CHECK_INTERVAL_MS),
                        fromEvent(this.document, 'visibilitychange'),
                        fromEvent(view, 'online'),
                    ),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                void this.checkForUpdateAsync();
            });
    }

    public dismiss(): void {
        if (this.recoveryRequired()) {
            return;
        }
        this.dismissedVersion.set(this.readyVersion());
        this.needsSave.set(false);
    }

    public async reloadAsync(confirmReload: () => Promise<boolean>): Promise<void> {
        if (!this.updateAvailable() || this.isReloading()) {
            return;
        }
        this.isReloading.set(true);
        try {
            if (this.hasUnsavedChanges()) {
                return;
            }
            if ((await confirmReload()) && this.updateAvailable()) {
                // A save/dialog can leave newer edits pending. Never bypass their owner.
                if (this.hasUnsavedChanges()) {
                    return;
                }
                this.document.defaultView?.location.reload();
            }
        } catch {
            this.needsSave.set(true);
        } finally {
            this.isReloading.set(false);
        }
    }

    private hasUnsavedChanges(): boolean {
        const hasChanges = this.unsavedChanges.getHandler()?.hasChanges() === true;
        this.needsSave.set(hasChanges);
        return hasChanges;
    }

    private async checkForUpdateAsync(): Promise<void> {
        const view = this.document.defaultView;
        if (view === null || this.document.visibilityState === 'hidden' || !view.navigator.onLine || this.checking) {
            return;
        }
        const now = Date.now();
        if (this.lastCheckAt !== null && now - this.lastCheckAt < RESUME_CHECK_INTERVAL_MS) {
            return;
        }
        this.checking = true;
        this.lastCheckAt = now;
        try {
            await this.swUpdate?.checkForUpdate();
        } catch {
            this.diagnostics.report('check');
        } finally {
            this.checking = false;
        }
    }
}
