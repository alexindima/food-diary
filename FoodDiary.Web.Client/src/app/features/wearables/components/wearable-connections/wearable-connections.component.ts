import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
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

    public readonly providerRows = this.facade.providerRows;

    public constructor() {
        this.facade.initialize();
    }

    public connect(providerId: string): void {
        const state = crypto.randomUUID();
        this.facade
            .getAuthUrl(providerId, state)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                window.location.href = result.authorizationUrl;
            });
    }

    public disconnect(providerId: string): void {
        this.facade.disconnect(providerId);
    }
}
