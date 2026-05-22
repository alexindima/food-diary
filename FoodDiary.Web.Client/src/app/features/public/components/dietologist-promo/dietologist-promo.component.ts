import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import type { PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { PublicAuthNavigationService } from '../../lib/public-auth-navigation.service';
import { DIETOLOGIST_PROMO_PERMISSIONS, DIETOLOGIST_PROMO_WORKFLOW_STEPS } from './dietologist-promo.config';

@Component({
    selector: 'fd-dietologist-promo',
    imports: [TranslateModule, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './dietologist-promo.component.html',
    styleUrl: './dietologist-promo.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistPromoComponent {
    private readonly authNavigationService = inject(PublicAuthNavigationService);

    protected readonly workflowSteps = DIETOLOGIST_PROMO_WORKFLOW_STEPS;
    protected readonly permissions = DIETOLOGIST_PROMO_PERMISSIONS;

    protected async navigateToAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authNavigationService.navigateAsync(mode);
    }
}
