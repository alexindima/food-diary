import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { environment } from '../../../../../../environments/environment';

@Component({
    selector: 'fd-premium-benefits-card',
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './premium-benefits-card.html',
    styleUrl: '../../premium-access/premium-access-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumBenefitsCardComponent {
    protected readonly supportEmail = environment.supportEmail ?? 'admin@fooddiary.club';
    protected readonly supportEmailHref = `mailto:${this.supportEmail}`;
}
