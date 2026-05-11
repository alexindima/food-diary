import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { BillingPlan } from '../models/billing.models';
import type { PremiumCheckoutRequest, PremiumPlanCardViewModel } from './premium-access-page.types';

@Component({
    selector: 'fd-premium-plans-card',
    imports: [FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './premium-plans-card.component.html',
    styleUrl: './premium-access-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PremiumPlansCardComponent {
    public readonly cards = input.required<PremiumPlanCardViewModel[]>();
    public readonly showProviderChoices = input.required<boolean>();
    public readonly checkoutLoadingPlan = input.required<BillingPlan | null>();

    public readonly checkout = output<PremiumCheckoutRequest>();
}
