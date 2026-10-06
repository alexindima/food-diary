import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInlineAlertComponent } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert';

import type { PremiumPlanPrices } from '../../../models/premium-plan-price';
import type { PremiumCheckoutRequest, PremiumPlanCardViewModel } from '../../premium-access/premium-access-lib/premium-access.types';
import { PremiumPlanPriceComponent } from './premium-plan-price';

@Component({
    selector: 'fd-premium-plans-card',
    imports: [FdUiButtonComponent, FdUiCardComponent, FdUiInlineAlertComponent, PremiumPlanPriceComponent, TranslatePipe],
    templateUrl: './premium-plans-card.html',
    styleUrl: '../../premium-access/premium-access-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumPlansCardComponent {
    public readonly cards = input.required<PremiumPlanCardViewModel[]>();
    public readonly prices = input<PremiumPlanPrices>({});
    public readonly pricesLoading = input(false);
    public readonly pricesUnavailable = input(false);
    protected readonly hasPrices = computed(() => Object.keys(this.prices()).length > 0);
    protected readonly showProviderChoices = computed(() => this.cards().some(card => card.providerOptions.length > 1));
    protected readonly checkoutDisabled = computed(() => this.cards().some(card => card.isLoading));

    public readonly checkout = output<PremiumCheckoutRequest>();
    public readonly retryPrices = output();
}
