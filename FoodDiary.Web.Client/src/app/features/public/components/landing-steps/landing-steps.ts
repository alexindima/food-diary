import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { PublicAuthNavigationService } from '../../lib/public-auth-navigation.service';
import { LANDING_STEPS } from './landing-steps.config';

@Component({
    selector: 'fd-landing-steps',
    imports: [TranslateModule, FdUiButtonComponent],
    templateUrl: './landing-steps.html',
    styleUrls: ['./landing-steps.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingStepsComponent {
    private readonly authNavigationService = inject(PublicAuthNavigationService);

    protected readonly steps = LANDING_STEPS;

    protected async navigateToAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authNavigationService.navigateAsync(mode);
    }
}
