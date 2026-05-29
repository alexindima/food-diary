import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { environment } from '../../../../../../environments/environment';

@Component({
    selector: 'fd-premium-benefits-card',
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './premium-benefits-card.component.html',
    styleUrl: '../../premium-access/premium-access-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumBenefitsCardComponent {
    protected readonly supportEmail = environment.supportEmail ?? 'admin@fooddiary.club';
    protected readonly supportEmailHref = `mailto:${this.supportEmail}`;
}
