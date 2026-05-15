import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { PremiumCheckoutRequest, PremiumPlanCardViewModel } from '../../premium-access/premium-access-lib/premium-access.types';

@Component({
    selector: 'fd-premium-plans-card',
    imports: [FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './premium-plans-card.component.html',
    styleUrl: '../../premium-access/premium-access-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumPlansCardComponent {
    public readonly cards = input.required<PremiumPlanCardViewModel[]>();
    public readonly showProviderChoices = computed(() => this.cards().some(card => card.providerOptions.length > 1));
    public readonly checkoutDisabled = computed(() => this.cards().some(card => card.isLoading));

    public readonly checkout = output<PremiumCheckoutRequest>();
}
