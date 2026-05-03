import { inject, Injectable } from '@angular/core';
import { type PreloadingStrategy, type Route } from '@angular/router';
import { EMPTY, from, type Observable, switchMap } from 'rxjs';

import { AuthService } from './auth.service';

type IdleCapableGlobal = typeof globalThis & {
    addEventListener?: (type: string, listener: EventListenerOrEventListenerObject, options?: AddEventListenerOptions | boolean) => void;
    removeEventListener?: (type: string, listener: EventListenerOrEventListenerObject, options?: EventListenerOptions | boolean) => void;
    dispatchEvent?: (event: Event) => boolean;
    requestIdleCallback?: (callback: () => void, options?: { timeout?: number }) => number;
    cancelIdleCallback?: (handle: number) => void;
    setTimeout: typeof setTimeout;
    clearTimeout: typeof clearTimeout;
    document?: Document;
};

@Injectable({ providedIn: 'root' })
export class IdleSelectivePreloadingStrategy implements PreloadingStrategy {
    private readonly authService = inject(AuthService);
    private readonly globalObject = globalThis as IdleCapableGlobal;
    private pageReadyPromise?: Promise<void>;

    public preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
        if (route.data?.['preload'] !== true) {
            return EMPTY;
        }

        const requiresAuth = (route.canActivate?.length ?? 0) > 0;
        if (requiresAuth && !this.authService.isAuthenticated()) {
            return EMPTY;
        }

        return from(this.waitForPageReady()).pipe(
            switchMap(() => from(this.waitForIdle())),
            switchMap(() => load()),
        );
    }

    private waitForPageReady(): Promise<void> {
        this.pageReadyPromise ??= new Promise(resolve => {
            const documentReadyState = this.globalObject.document?.readyState;
            if (documentReadyState === 'complete') {
                resolve();
                return;
            }

            if (typeof this.globalObject.addEventListener !== 'function' || typeof this.globalObject.removeEventListener !== 'function') {
                this.globalObject.setTimeout(() => {
                    resolve();
                }, 1500);
                return;
            }

            const onLoad = (): void => {
                this.globalObject.removeEventListener?.('load', onLoad);
                resolve();
            };

            this.globalObject.addEventListener('load', onLoad, { once: true });

            // Fallback for browsers where the load event never reaches this listener path.
            this.globalObject.setTimeout(() => {
                this.globalObject.removeEventListener?.('load', onLoad);
                resolve();
            }, 3000);
        });

        return this.pageReadyPromise;
    }

    private waitForIdle(): Promise<void> {
        return new Promise(resolve => {
            if (typeof this.globalObject.requestIdleCallback === 'function') {
                this.globalObject.requestIdleCallback(
                    () => {
                        resolve();
                    },
                    { timeout: 2000 },
                );
                return;
            }

            this.globalObject.setTimeout(() => {
                resolve();
            }, 1200);
        });
    }
}
