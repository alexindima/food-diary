import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import { PublicAuthDialogService, type PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { DIETOLOGIST_PROMO_PERMISSIONS, DIETOLOGIST_PROMO_WORKFLOW_STEPS } from './dietologist-promo.config';

@Component({
    selector: 'fd-dietologist-promo',
    imports: [TranslateModule, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './dietologist-promo.component.html',
    styleUrl: './dietologist-promo.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistPromoComponent {
    private readonly authDialogService = inject(PublicAuthDialogService);

    protected readonly workflowSteps = DIETOLOGIST_PROMO_WORKFLOW_STEPS;
    protected readonly permissions = DIETOLOGIST_PROMO_PERMISSIONS;

    protected async openAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authDialogService.openAsync({ mode });
    }
}
