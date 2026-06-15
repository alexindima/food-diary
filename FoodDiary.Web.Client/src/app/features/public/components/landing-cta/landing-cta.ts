import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { PublicAuthNavigationService } from '../../lib/public-auth-navigation.service';

@Component({
    selector: 'fd-landing-cta',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './landing-cta.html',
    styleUrls: ['./landing-cta.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingCtaComponent {
    private readonly authNavigationService = inject(PublicAuthNavigationService);

    protected async navigateToAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authNavigationService.navigateAsync(mode);
    }
}
