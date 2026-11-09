import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

@Component({
    selector: 'fd-premium-benefits-card',
    imports: [FdUiCardComponent, FdUiIconComponent, TranslatePipe],
    templateUrl: './premium-benefits-card.html',
    styleUrl: '../../premium-access/premium-access-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumBenefitsCardComponent {
    public readonly accessActive = input(false);
    protected readonly benefits = [
        { key: 'PREMIUM_PAGE.BENEFITS.COACH', icon: 'auto_awesome' },
        { key: 'PREMIUM_PAGE.BENEFITS.ANALYTICS', icon: 'insights' },
        { key: 'PREMIUM_PAGE.BENEFITS.REPORTS', icon: 'summarize' },
    ];
}
