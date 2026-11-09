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
    public readonly starsPrice = input<number | null>(null);
    public readonly preferTrial = input(false);
    protected readonly hasPrices = computed(() => Object.keys(this.prices()).length > 0);
    protected readonly showProviderChoices = computed(() => this.cards().some(card => card.providerOptions.length > 1));
    protected readonly checkoutDisabled = computed(() => this.cards().some(card => card.isLoading));
    protected readonly singlePlan = computed(() => this.cards().length === 1);
    protected readonly hasStarsPlan = computed(() => this.cards().some(card => this.supportsStars(card)));

    public readonly checkout = output<PremiumCheckoutRequest>();
    public readonly retryPrices = output();
    public readonly readTerms = output();

    protected starsPriceFor(card: PremiumPlanCardViewModel): number | null {
        return this.supportsStars(card) ? this.starsPrice() : null;
    }

    private supportsStars(card: PremiumPlanCardViewModel): boolean {
        return card.providerOptions.some(option => option.provider.toLowerCase() === 'telegramstars');
    }
}
