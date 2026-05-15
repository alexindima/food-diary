import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { BillingOverview } from '../../../models/billing.models';
import type { PremiumOverviewCardViewModel } from '../../premium-access/premium-access-lib/premium-access.types';

@Component({
    selector: 'fd-premium-overview-card',
    imports: [FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './premium-overview-card.component.html',
    styleUrl: '../../premium-access/premium-access-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumOverviewCardComponent {
    public readonly viewModel = input.required<PremiumOverviewCardViewModel>();
    public readonly portalLoading = input.required<boolean>();
    public readonly isLoading = input.required<boolean>();
    public readonly overview = input.required<BillingOverview | null>();

    public readonly manageBilling = output();
}
