import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import type { BillingOverview } from '../../../../../shared/models/billing.models';
import { PremiumCancellationActionsComponent } from '../../../components/cancellation-actions/premium-cancellation-actions';
import type { PremiumOverviewCardViewModel } from '../../premium-access/premium-access-lib/premium-access.types';

@Component({
    selector: 'fd-premium-overview-card',
    imports: [PremiumCancellationActionsComponent, FdUiButtonComponent, FdUiCardComponent, FdUiIconComponent, TranslatePipe],
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
    public readonly plansAvailable = input(false);

    public readonly overviewChanged = output<BillingOverview>();
    public readonly manageBilling = output();
    public readonly startTrial = output();
    public readonly resumeSubscription = output();
    public readonly readSupport = output();

    protected readonly showTrialOffer = computed(() => this.viewModel().isFreePlan && this.viewModel().showStartTrial);
    protected readonly showResume = computed(() => this.viewModel().subscriptionEnded && this.plansAvailable());
    protected readonly showPaymentHelp = computed(() => this.viewModel().paymentIssue && !this.viewModel().showManageBilling);
    protected readonly showActions = computed(
        () =>
            this.viewModel().showManageBilling ||
            this.viewModel().showStartTrial ||
            this.showResume() ||
            this.showPaymentHelp() ||
            this.overview()?.cancellationAvailable === true,
    );
    protected readonly manageActionKey = computed(() => {
        if (this.viewModel().paymentIssue) {
            return 'PREMIUM_PAGE.OVERVIEW.FIX_PAYMENT_ACTION';
        }
        const overview = this.overview();
        return overview?.cancellationAvailable === true ||
            overview?.cancelAtPeriodEnd === true ||
            overview?.subscriptionStatus === 'canceled'
            ? 'PREMIUM_PAGE.CANCELLATION.PAYMENTS_ACTION'
            : 'PREMIUM_PAGE.OVERVIEW.MANAGE_ACTION';
    });
}
