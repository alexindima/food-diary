import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInlineAlertComponent } from 'fd-ui-kit';

import { PwaInstallService } from '../../../shared/service-worker/pwa-install.service';
import { PwaInstallPromotionService } from '../../../shared/service-worker/pwa-install-promotion.service';

@Component({
    selector: 'fd-pwa-install-actions',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiInlineAlertComponent],
    templateUrl: './pwa-install-actions.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PwaInstallActionsComponent {
    public readonly instructionsId = input.required<string>();
    protected readonly pwa = inject(PwaInstallService);
    protected readonly promotion = inject(PwaInstallPromotionService);
    protected readonly showIosInstructions = signal(false);
}
