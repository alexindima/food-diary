import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-premium-benefits-card',
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './premium-benefits-card.component.html',
    styleUrl: './premium-access-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumBenefitsCardComponent {}
