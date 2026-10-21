import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { PwaInstallService } from '../../../shared/service-worker/pwa-install.service';
import { PwaInstallPromotionService } from '../../../shared/service-worker/pwa-install-promotion.service';
import { PwaInstallActionsComponent } from '../pwa-install-actions/pwa-install-actions';

@Component({
    selector: 'fd-pwa-install-card',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent, PwaInstallActionsComponent],
    templateUrl: './pwa-install-card.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PwaInstallCardComponent {
    protected readonly pwa = inject(PwaInstallService);
    protected readonly promotion = inject(PwaInstallPromotionService);
}
