import { ChangeDetectionStrategy, Component, effect, model, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { environment } from '../../../../../../environments/environment';

@Component({
    selector: 'fd-premium-support-card',
    imports: [FdUiCardComponent, FdUiButtonComponent, FdUiIconComponent, TranslatePipe],
    templateUrl: './premium-support-card.html',
    styleUrl: '../../premium-access/premium-access-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumSupportCardComponent {
    private readonly termsToggle = viewChild<FdUiButtonComponent>('termsToggle');
    public readonly expanded = model(false);
    protected readonly supportEmail = environment.supportEmail ?? 'admin@fooddiary.club';
    protected readonly supportEmailHref = `mailto:${this.supportEmail}`;

    public constructor() {
        effect(() => {
            if (this.expanded()) {
                this.termsToggle()?.focus();
            }
        });
    }
}
