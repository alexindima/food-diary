import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { PublicAuthNavigationService } from '../../lib/public-auth-navigation.service';

@Component({
    selector: 'fd-landing-cta',
    imports: [TranslateModule, FdUiButtonComponent],
    templateUrl: './landing-cta.component.html',
    styleUrls: ['./landing-cta.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingCtaComponent {
    private readonly authNavigationService = inject(PublicAuthNavigationService);

    protected async navigateToAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authNavigationService.navigateAsync(mode);
    }
}
