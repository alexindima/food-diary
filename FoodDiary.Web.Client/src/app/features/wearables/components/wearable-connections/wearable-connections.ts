import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiInlineAlertComponent } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { WearableConnectionsFacade } from '../../lib/wearable-connections.facade';

@Component({
    selector: 'fd-wearable-connections',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiInlineAlertComponent, FdUiLoaderComponent],
    templateUrl: './wearable-connections.html',
    styleUrls: ['./wearable-connections.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WearableConnectionsFacade],
})
export class WearableConnectionsComponent {
    private readonly facade = inject(WearableConnectionsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly browserWindow = inject(BrowserWindowService);

    protected readonly providerRows = this.facade.providerRows;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly hasLoadError = this.facade.hasLoadError;

    protected retryLoad(): void {
        this.facade.retryLoad();
    }

    protected connect(providerId: string): void {
        const state = crypto.randomUUID();
        this.facade
            .getAuthUrl(providerId, state)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(({ authorizationUrl }) => {
                this.browserWindow.navigateCurrentTab(authorizationUrl);
            });
    }

    protected disconnect(providerId: string): void {
        this.facade.disconnect(providerId);
    }
}
