import { BreakpointObserver } from '@angular/cdk/layout';
import { computed, inject, Service } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { distinctUntilChanged, map } from 'rxjs';

import { APP_MOBILE_VIEWPORT_QUERY } from '../../config/runtime-ui.tokens';
import { BrowserWindowService } from './browser-window.service';

@Service()
export class ViewportService {
    private readonly breakpointObserver = inject(BreakpointObserver);
    private readonly browserWindow = inject(BrowserWindowService);
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
        return this.browserWindow.matchMedia(this.mobileViewportQuery)?.matches ?? false;
    }
}
