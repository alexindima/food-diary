import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { PublicAuthDialogService, type PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { LANDING_STEPS } from './landing-steps.config';

@Component({
    selector: 'fd-landing-steps',
    imports: [TranslateModule, FdUiButtonComponent],
    templateUrl: './landing-steps.component.html',
    styleUrls: ['./landing-steps.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingStepsComponent {
    private readonly authDialogService = inject(PublicAuthDialogService);

    protected readonly steps = LANDING_STEPS;

    public async openAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authDialogService.openAsync({ mode });
    }
}
