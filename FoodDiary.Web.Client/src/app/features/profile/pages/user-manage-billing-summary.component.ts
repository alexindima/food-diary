import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiStatusBadgeComponent } from 'fd-ui-kit/status-badge/fd-ui-status-badge.component';

import type { BillingViewModel } from './user-manage.types';

@Component({
    selector: 'fd-user-manage-billing-summary',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiStatusBadgeComponent],
    templateUrl: './user-manage-billing-summary.component.html',
    styleUrl: './user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageBillingSummaryComponent {
    public readonly billing = input.required<BillingViewModel>();
    public readonly billingPlanLabelKey = input.required<string>();
    public readonly billingStatusLabelKey = input.required<string>();
    public readonly billingProviderLabel = input.required<string>();
    public readonly billingRenewalLabelKey = input.required<string>();
    public readonly billingCurrentPeriodStartLabel = input.required<string | null>();
    public readonly billingCurrentPeriodEndLabel = input.required<string | null>();
    public readonly billingNextAttemptLabel = input.required<string | null>();
    public readonly isOpeningBillingPortal = input.required<boolean>();

    public readonly billingPortalOpen = output();
    public readonly premiumPageOpen = output();
}
