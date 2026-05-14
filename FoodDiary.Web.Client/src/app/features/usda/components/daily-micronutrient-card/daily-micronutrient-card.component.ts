import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { buildDailyMicronutrientViews } from '../../lib/usda-micronutrient.mapper';
import type { DailyMicronutrient } from '../../models/usda.data';

@Component({
    selector: 'fd-daily-micronutrient-card',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './daily-micronutrient-card.component.html',
    styleUrls: ['./daily-micronutrient-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailyMicronutrientCardComponent {
    public readonly nutrients = input<DailyMicronutrient[]>([]);
    public readonly linkedCount = input(0);
    public readonly totalCount = input(0);

    public readonly keyNutrients = computed(() => buildDailyMicronutrientViews(this.nutrients()));
}
