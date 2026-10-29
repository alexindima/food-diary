import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { HYDRATION_CARD_ADD_AMOUNTS_ML } from '../hydration-card.config';

@Component({
    selector: 'fd-hydration-card-actions',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './hydration-card-actions.html',
    styleUrl: './hydration-card-actions.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HydrationCardActionsComponent {
    public readonly canAdd = input.required<boolean>();
    public readonly isLoading = input.required<boolean>();
    public readonly addedAmount = input.required<number | null>();
    public readonly addClick = output<number>();
    protected readonly addAmounts = HYDRATION_CARD_ADD_AMOUNTS_ML;
}
