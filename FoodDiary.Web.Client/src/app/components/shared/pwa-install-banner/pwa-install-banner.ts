import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { AppUpdateService } from '../../../shared/service-worker/app-update.service';
import { PwaInstallPromotionService } from '../../../shared/service-worker/pwa-install-promotion.service';
import { PwaInstallActionsComponent } from '../pwa-install-actions/pwa-install-actions';

@Component({
    selector: 'fd-pwa-install-banner',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent, PwaInstallActionsComponent],
    templateUrl: './pwa-install-banner.html',
    styleUrl: './pwa-install-banner.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PwaInstallBannerComponent {
    public readonly visible = input.required<boolean>();
    protected readonly promotion = inject(PwaInstallPromotionService);
    protected readonly updates = inject(AppUpdateService);
}
