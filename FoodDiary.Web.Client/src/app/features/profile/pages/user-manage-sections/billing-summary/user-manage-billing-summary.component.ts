import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiStatusBadgeComponent } from 'fd-ui-kit/status-badge/fd-ui-status-badge.component';

import { LocalizationService } from '../../../../../services/localization.service';
import type { BillingViewModel } from '../../user-manage/user-manage.types';
import {
    getBillingPlanLabelKey,
    getBillingProviderLabel,
    getBillingRenewalLabelKey,
    getBillingStatusLabelKey,
} from '../../user-manage/user-manage-billing.mapper';
import { formatUserManageDate } from '../../user-manage/user-manage-date.mapper';

@Component({
    selector: 'fd-user-manage-billing-summary',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiStatusBadgeComponent],
    templateUrl: './user-manage-billing-summary.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageBillingSummaryComponent {
    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);

    public readonly billing = input.required<BillingViewModel>();
    public readonly isOpeningBillingPortal = input.required<boolean>();

    public readonly billingPlanLabelKey = computed(() => getBillingPlanLabelKey(this.billing().overview));
    public readonly billingStatusLabelKey = computed(() => getBillingStatusLabelKey(this.billing().overview));
    public readonly billingProviderLabel = computed(() =>
        getBillingProviderLabel(this.billing().overview.subscriptionProvider ?? this.billing().overview.provider, key =>
            this.translateService.instant(key),
        ),
    );
    public readonly billingRenewalLabelKey = computed(() => getBillingRenewalLabelKey(this.billing().overview));

    public readonly billingPortalOpen = output();
    public readonly premiumPageOpen = output();

    public formatDate(value: string | null | undefined): string | null {
        return formatUserManageDate(value, this.localizationService.getCurrentLanguage());
    }
}
