import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { BillingViewModel } from '../../user-manage/user-manage.types';
import { UserManageBillingSummaryComponent } from '../billing-summary/user-manage-billing-summary.component';

@Component({
    selector: 'fd-user-manage-billing-card',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent, UserManageBillingSummaryComponent],
    templateUrl: './user-manage-billing-card.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageBillingCardComponent {
    public readonly isLoadingBilling = input.required<boolean>();
    public readonly billingError = input.required<string | null>();
    public readonly billingView = input.required<BillingViewModel | null>();
    public readonly billingCurrentPeriodStartLabel = input.required<string | null>();
    public readonly billingCurrentPeriodEndLabel = input.required<string | null>();
    public readonly billingNextAttemptLabel = input.required<string | null>();
    public readonly isOpeningBillingPortal = input.required<boolean>();

    public readonly billingReload = output();
    public readonly billingPortalOpen = output();
    public readonly premiumPageOpen = output();
}
