import { BreakpointObserver } from '@angular/cdk/layout';
import { isPlatformBrowser } from '@angular/common';
import { computed, inject, Injectable, PLATFORM_ID } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { distinctUntilChanged, map } from 'rxjs';

import { APP_MOBILE_VIEWPORT_QUERY } from '../config/runtime-ui.tokens';

@Injectable({ providedIn: 'root' })
export class ViewportService {
    private readonly breakpointObserver = inject(BreakpointObserver);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly mobileViewportQuery = inject(APP_MOBILE_VIEWPORT_QUERY);
    private readonly mobileMatch = toSignal(
        this.breakpointObserver.observe(this.mobileViewportQuery).pipe(
            map(result => result.matches),
            distinctUntilChanged(),
        ),
        { initialValue: this.getInitialMobileMatch() },
    );

    public readonly isMobile = computed(() => this.mobileMatch());

    private getInitialMobileMatch(): boolean {
        if (!isPlatformBrowser(this.platformId)) {
            return false;
        }

        return window.matchMedia(this.mobileViewportQuery).matches;
    }
}
