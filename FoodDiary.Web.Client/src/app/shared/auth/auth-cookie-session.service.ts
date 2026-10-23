import { inject, Service } from '@angular/core';
import { finalize, Observable, type Subscription } from 'rxjs';

import { BrowserWindowService } from '../platform/browser-window.service';

const AUTH_COOKIE_LOCK = 'fooddiary.auth.cookie-session';

@Service()
export class AuthCookieSessionService {
    private readonly browser = inject(BrowserWindowService);
    private readonly queue: Array<() => void> = [];
    private running = false;

    public run<T>(request: () => Observable<T>): Observable<T> {
        return new Observable(subscriber => {
            let active: Subscription | null = null;
            let release: () => void = () => {};
            this.queue.push(() => {
                let completed = false;
                const finish = (): void => {
                    if (completed) {
                        return;
                    }
                    completed = true;
                    release();
                    this.running = false;
                    this.startNext();
                };
                void this.browser
                    .runWithLockAsync(
                        AUTH_COOKIE_LOCK,
                        async () =>
                            new Promise<void>(resolve => {
                                release = resolve;
                                if (subscriber.closed) {
                                    finish();
                                    return;
                                }
                                try {
                                    active = request().pipe(finalize(finish)).subscribe(subscriber);
                                } catch (error: unknown) {
                                    subscriber.error(error);
                                    finish();
                                }
                            }),
                    )
                    .catch((error: unknown) => {
                        subscriber.error(error);
                        finish();
                    });
            });
            this.startNext();
            return () => {
                active?.unsubscribe();
                release();
            };
        });
    }

    private startNext(): void {
        if (this.running) {
            return;
        }
        const next = this.queue.shift();
        if (next !== undefined) {
            this.running = true;
            next();
        }
    }
}
