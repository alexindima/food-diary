import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { BillingPlan } from '../../../../../shared/models/billing.models';
import type { PremiumPlanPrice } from '../../../models/premium-plan-price';

@Component({
    selector: 'fd-premium-plan-price',
    imports: [TranslatePipe],
    templateUrl: './premium-plan-price.html',
    styleUrl: '../../premium-access/premium-access-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumPlanPriceComponent {
    public readonly plan = input.required<BillingPlan>();
    public readonly price = input<PremiumPlanPrice | null>(null);
    public readonly loading = input(false);
    public readonly showProvider = input(false);
    protected readonly periodKey = computed(() =>
        this.plan() === 'monthly' ? 'PREMIUM_PAGE.PLANS.PRICE_MONTHLY_PERIOD' : 'PREMIUM_PAGE.PLANS.PRICE_YEARLY_PERIOD',
    );
}
