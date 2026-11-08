import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { computed, DestroyRef, effect, inject, Injector, PLATFORM_ID, Service, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { UserService } from '../api/user.service';
import { BrowserSessionStateService } from './browser-session-state.service';

const ACTIVITY_INTERVAL_MS = 300_000;

@Service()
export class UserActivityService {
    private readonly auth = inject(AuthService);
    private readonly users = inject(UserService);
    private readonly browserSession = inject(BrowserSessionStateService);
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly identity = computed(() =>
        this.auth.isAuthenticated() && !this.auth.isImpersonating() && this.browserSession.contextId() === null
            ? this.auth.getUserId()
            : null,
    );
    private initialized = false;
    private currentUserId: string | null = null;
    private lastAttemptAt: number | null = null;

    public initialize(): void {
        if (!this.isBrowser || this.initialized) {
            return;
        }
        this.initialized = true;
        // Observe identity changes only: refreshing the same session is background work.
        effect(
            () => {
                this.currentUserId = this.identity();
                this.lastAttemptAt = null;
                untracked(() => {
                    this.recordForegroundActivity();
                });
            },
            { injector: this.injector },
        );
        const record = (): void => {
            this.recordForegroundActivity();
        };
        const events = ['pointerdown', 'keydown', 'wheel', 'visibilitychange'] as const;
        events.forEach(event => {
            this.document.addEventListener(event, record, { passive: true, capture: true });
        });
        this.destroyRef.onDestroy(() => {
            events.forEach(event => {
                this.document.removeEventListener(event, record, { capture: true });
            });
        });
    }

    private recordForegroundActivity(): void {
        if (this.document.visibilityState !== 'visible' || this.currentUserId === null || this.identity() !== this.currentUserId) {
            return;
        }
        const now = Date.now();
        if (this.lastAttemptAt !== null && now - this.lastAttemptAt < ACTIVITY_INTERVAL_MS) {
            return;
        }
        this.lastAttemptAt = now;
        // Activity is best effort. Retry only after another foreground event and the throttle interval.
        this.users
            .recordActivity()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                catchError(() => EMPTY),
            )
            .subscribe();
    }
}
