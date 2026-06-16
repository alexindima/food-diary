import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, InjectionToken, PLATFORM_ID, Service } from '@angular/core';

const FD_TOUR_STORAGE_PREFIX = 'fd-tour';

export type FdTourStorage = {
    isCompleted: (tourId: string, version: number) => boolean;
    markCompleted: (tourId: string, version: number) => void;
    clearCompleted: (tourId: string) => void;
};

@Service()
export class FdTourLocalStorage implements FdTourStorage {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);

    public isCompleted(tourId: string, version: number): boolean {
        return this.getStorage()?.getItem(this.buildKey(tourId)) === String(version);
    }

    public markCompleted(tourId: string, version: number): void {
        try {
            this.getStorage()?.setItem(this.buildKey(tourId), String(version));
        } catch {
            // Tour persistence is optional and should not break onboarding.
        }
    }

    public clearCompleted(tourId: string): void {
        try {
            this.getStorage()?.removeItem(this.buildKey(tourId));
        } catch {
            // Tour persistence is optional and should not break onboarding.
        }
    }

    private getStorage(): Storage | null {
        if (!isPlatformBrowser(this.platformId)) {
            return null;
        }

        return this.document.defaultView?.localStorage ?? null;
    }

    private buildKey(tourId: string): string {
        return `${FD_TOUR_STORAGE_PREFIX}:${tourId}`;
    }
}

export const FD_TOUR_STORAGE = new InjectionToken<FdTourStorage>('FD_TOUR_STORAGE', {
    providedIn: 'root',
    factory: (): FdTourStorage => inject(FdTourLocalStorage),
});
