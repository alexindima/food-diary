import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { PublicAuthDialogService, type PublicAuthMode } from '../../lib/public-auth-dialog.service';

@Component({
    selector: 'fd-landing-cta',
    imports: [TranslateModule, FdUiButtonComponent],
    templateUrl: './landing-cta.component.html',
    styleUrls: ['./landing-cta.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingCtaComponent {
    private readonly authDialogService = inject(PublicAuthDialogService);

    public async openAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authDialogService.openAsync({ mode });
    }
}
