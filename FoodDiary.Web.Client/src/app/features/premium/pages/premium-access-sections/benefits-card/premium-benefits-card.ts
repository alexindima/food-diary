import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

@Component({
    selector: 'fd-premium-benefits-card',
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './premium-benefits-card.html',
    styleUrl: '../../premium-access/premium-access-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumBenefitsCardComponent {}
