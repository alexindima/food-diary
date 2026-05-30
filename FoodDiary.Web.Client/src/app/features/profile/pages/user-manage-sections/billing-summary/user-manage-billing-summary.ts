import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiStatusBadgeComponent } from 'fd-ui-kit/status-badge/fd-ui-status-badge';

import { LocalizationService } from '../../../../../services/localization.service';
import type { BillingViewModel } from '../../user-manage/user-manage-lib/user-manage.types';
import {
    getBillingPlanLabelKey,
    getBillingProviderLabel,
    getBillingRenewalLabelKey,
    getBillingStatusLabelKey,
} from '../../user-manage/user-manage-lib/user-manage-billing.mapper';
import { formatUserManageDate } from '../../user-manage/user-manage-lib/user-manage-date.mapper';

@Component({
    selector: 'fd-user-manage-billing-summary',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiStatusBadgeComponent],
    templateUrl: './user-manage-billing-summary.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageBillingSummaryComponent {
    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);

    public readonly billing = input.required<BillingViewModel>();
    public readonly isOpeningBillingPortal = input.required<boolean>();

    protected readonly billingPlanLabelKey = computed(() => getBillingPlanLabelKey(this.billing().overview));
    protected readonly billingStatusLabelKey = computed(() => getBillingStatusLabelKey(this.billing().overview));
    protected readonly billingProviderLabel = computed(() =>
        getBillingProviderLabel(this.billing().overview.subscriptionProvider ?? this.billing().overview.provider, key =>
            this.translateService.instant(key),
        ),
    );
    protected readonly billingRenewalLabelKey = computed(() => getBillingRenewalLabelKey(this.billing().overview));

    public readonly billingPortalOpen = output();
    public readonly premiumPageOpen = output();

    protected formatDate(value: string | null | undefined): string | null {
        return formatUserManageDate(value, this.localizationService.getCurrentLanguage());
    }
}
