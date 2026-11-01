import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { BillingOverview } from '../../../../../shared/models/billing.models';
import { PremiumCancellationActionsComponent } from '../../../components/cancellation-actions/premium-cancellation-actions';
import type { PremiumOverviewCardViewModel } from '../../premium-access/premium-access-lib/premium-access.types';

@Component({
    selector: 'fd-premium-overview-card',
    imports: [PremiumCancellationActionsComponent, FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './premium-overview-card.html',
    styleUrl: '../../premium-access/premium-access-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumOverviewCardComponent {
    public readonly viewModel = input.required<PremiumOverviewCardViewModel>();
    public readonly portalLoading = input.required<boolean>();
    public readonly trialLoading = input.required<boolean>();
    public readonly isLoading = input.required<boolean>();
    public readonly overview = input.required<BillingOverview | null>();

    public readonly overviewChanged = output<BillingOverview>();
    public readonly manageBilling = output();
    public readonly startTrial = output();
}
