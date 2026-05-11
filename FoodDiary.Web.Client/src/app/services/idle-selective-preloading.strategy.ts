import { inject, Injectable } from '@angular/core';
import type { PreloadingStrategy, Route } from '@angular/router';
import { EMPTY, from, type Observable, switchMap } from 'rxjs';

import { AuthService } from './auth.service';

const PAGE_READY_FALLBACK_MS = 1500;
const LOAD_EVENT_FALLBACK_MS = 3000;
const IDLE_TIMEOUT_MS = 2000;
const IDLE_FALLBACK_MS = 1200;

type IdleCapableGlobal = {
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
    private readonly globalObject = globalThis as unknown as IdleCapableGlobal;
    private pageReadyPromise?: Promise<void>;

    public preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
        if (route.data?.['preload'] !== true) {
            return EMPTY;
        }

        const requiresAuth = (route.canActivate?.length ?? 0) > 0;
        if (requiresAuth && !this.authService.isAuthenticated()) {
            return EMPTY;
        }

        return from(this.waitForPageReadyAsync()).pipe(
            switchMap(() => from(this.waitForIdleAsync())),
            switchMap(() => load()),
        );
    }

    private waitForPageReadyAsync(): Promise<void> {
        this.pageReadyPromise ??= new Promise(resolve => {
            const documentReadyState = this.globalObject.document?.readyState;
            if (documentReadyState === 'complete') {
                resolve();
                return;
            }

            if (typeof this.globalObject.addEventListener !== 'function' || typeof this.globalObject.removeEventListener !== 'function') {
                this.globalObject.setTimeout(() => {
                    resolve();
                }, PAGE_READY_FALLBACK_MS);
                return;
            }

            const addEventListener = this.globalObject.addEventListener.bind(this.globalObject);
            const removeEventListener = this.globalObject.removeEventListener.bind(this.globalObject);

            const onLoad = (): void => {
                removeEventListener('load', onLoad);
                resolve();
            };

            addEventListener('load', onLoad, { once: true });

            // Fallback for browsers where the load event never reaches this listener path.
            this.globalObject.setTimeout(() => {
                removeEventListener('load', onLoad);
                resolve();
            }, LOAD_EVENT_FALLBACK_MS);
        });

        return this.pageReadyPromise;
    }

    private waitForIdleAsync(): Promise<void> {
        return new Promise(resolve => {
            if (typeof this.globalObject.requestIdleCallback === 'function') {
                this.globalObject.requestIdleCallback(
                    () => {
                        resolve();
                    },
                    { timeout: IDLE_TIMEOUT_MS },
                );
                return;
            }

            this.globalObject.setTimeout(() => {
                resolve();
            }, IDLE_FALLBACK_MS);
        });
    }
}
