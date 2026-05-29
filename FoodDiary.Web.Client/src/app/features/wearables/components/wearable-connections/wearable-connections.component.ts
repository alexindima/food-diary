import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, PLATFORM_ID } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { WearableConnectionsFacade } from '../../lib/wearable-connections.facade';

@Component({
    selector: 'fd-wearable-connections',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './wearable-connections.component.html',
    styleUrls: ['./wearable-connections.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WearableConnectionsComponent {
    private readonly facade = inject(WearableConnectionsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);

    protected readonly providerRows = this.facade.providerRows;

    public constructor() {
        this.facade.initialize();
    }

    protected connect(providerId: string): void {
        const state = crypto.randomUUID();
        this.facade
            .getAuthUrl(providerId, state)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                if (this.isBrowser) {
                    this.document.location.href = result.authorizationUrl;
                }
            });
    }

    protected disconnect(providerId: string): void {
        this.facade.disconnect(providerId);
    }
}
