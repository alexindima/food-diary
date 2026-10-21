import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ApplicationRef, computed, DestroyRef, inject, PLATFORM_ID, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, fromEvent, interval, switchMap, take } from 'rxjs';

import { BrowserStorageService } from '../platform/browser-storage.service';
import { PwaInstallService } from './pwa-install.service';
import {
    parseInstallPromotionState,
    PWA_PROMOTION_MIN_VISITS,
    PWA_PROMOTION_SNOOZE_MS,
    PWA_PROMOTION_STORAGE_KEY,
    PWA_PROMOTION_VISITS_STORAGE_KEY,
    type PwaInstallPromotionState,
    recordInstallPromotionVisit,
} from './pwa-install-promotion-state';

const ACTIVITY_INTERVAL_MS = 60_000;

@Service()
export class PwaInstallPromotionService {
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly appRef = inject(ApplicationRef);
    private readonly destroyRef = inject(DestroyRef);
    private readonly storage = inject(BrowserStorageService);
    private readonly pwa = inject(PwaInstallService);
    private readonly state = signal(parseInstallPromotionState(null));
    private readonly now = signal(Date.now());
    private initialized = false;
    private active = false;

    public readonly isDismissed = computed(() => this.state().never || (this.state().snoozedUntil ?? 0) > this.now());
    public readonly shouldShowBanner = computed(
        () =>
            this.pwa.isMobileDevice() &&
            this.pwa.showInstallCard() &&
            this.state().visits >= PWA_PROMOTION_MIN_VISITS &&
            !this.isDismissed(),
    );

    public initialize(): void {
        if (this.initialized || !this.isBrowser) {
            return;
        }
        const view = this.document.defaultView;
        if (view === null) {
            return;
        }
        this.initialized = true;
        this.refreshState();
        fromEvent<StorageEvent>(view, 'storage')
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(event => {
                if (event.key === PWA_PROMOTION_STORAGE_KEY || event.key === PWA_PROMOTION_VISITS_STORAGE_KEY || event.key === null) {
                    this.refreshState();
                }
            });
        fromEvent(this.document, 'visibilitychange')
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.recordActivity(this.document.visibilityState !== 'hidden');
            });
        this.appRef.isStable
            .pipe(
                filter(Boolean),
                take(1),
                switchMap(() => interval(ACTIVITY_INTERVAL_MS)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.recordActivity();
            });
    }

    public setActive(active: boolean): void {
        if (!this.initialized || this.active === active) {
            return;
        }
        if (this.active) {
            this.recordActivity(false);
        }
        this.active = active;
        if (active) {
            this.recordActivity();
        }
    }

    public snooze(): void {
        const now = Date.now();
        this.now.set(now);
        this.savePreferences({ ...this.readState(), snoozedUntil: now + PWA_PROMOTION_SNOOZE_MS });
    }

    public dismissPermanently(): void {
        this.savePreferences({ ...this.readState(), never: true, snoozedUntil: null });
    }

    public allowOffers(): void {
        this.savePreferences({ ...this.readState(), never: false, snoozedUntil: null });
    }

    public async installAsync(): Promise<void> {
        if ((await this.pwa.installAsync()) === 'dismissed') {
            this.snooze();
        }
    }

    private recordActivity(countVisit = true): void {
        if (!this.active || !this.pwa.isMobileDevice() || this.pwa.isStandalone()) {
            return;
        }
        if (countVisit && this.document.visibilityState === 'hidden') {
            return;
        }
        const now = Date.now();
        this.now.set(now);
        const state = this.readState();
        if (state.never) {
            this.state.set(state);
            return;
        }
        const next = countVisit ? recordInstallPromotionVisit(state, now) : { ...state, lastSeenAt: now };
        this.state.set(next);
        // Activity must never overwrite an opt-out saved concurrently in another tab.
        this.storage.setJson('local', PWA_PROMOTION_VISITS_STORAGE_KEY, { visits: next.visits, lastSeenAt: next.lastSeenAt });
    }

    private readState(fallback = this.state()): PwaInstallPromotionState {
        const storedPreferences = this.storage.getJson('local', PWA_PROMOTION_STORAGE_KEY);
        const storedVisits = this.storage.getJson('local', PWA_PROMOTION_VISITS_STORAGE_KEY);
        // Preserve this document's choices if storage is unavailable.
        const preferences = storedPreferences === null ? fallback : parseInstallPromotionState(storedPreferences);
        const visits = storedVisits === null ? fallback : parseInstallPromotionState(storedVisits);
        return { visits: visits.visits, lastSeenAt: visits.lastSeenAt, snoozedUntil: preferences.snoozedUntil, never: preferences.never };
    }

    private refreshState(): void {
        this.now.set(Date.now());
        this.state.set(this.readState(parseInstallPromotionState(null)));
    }

    private savePreferences(state: PwaInstallPromotionState): void {
        this.state.set(state);
        this.storage.setJson('local', PWA_PROMOTION_STORAGE_KEY, { never: state.never, snoozedUntil: state.snoozedUntil });
    }
}
