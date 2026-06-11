import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { CycleNutritionSummaryViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-nutrition-summary-card',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './cycle-nutrition-summary-card.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleNutritionSummaryCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly summary = input.required<CycleNutritionSummaryViewModel | null>();
}
