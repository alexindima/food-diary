import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { DAILY_MICRONUTRIENT_IDS } from '../../lib/usda-nutrient.constants';
import type { DailyMicronutrient } from '../../models/usda.data';

type DailyMicronutrientView = DailyMicronutrient & {
    percentDailyValueWidth: number | null;
};

@Component({
    selector: 'fd-daily-micronutrient-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './daily-micronutrient-card.component.html',
    styleUrls: ['./daily-micronutrient-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailyMicronutrientCardComponent {
    public readonly nutrients = input<DailyMicronutrient[]>([]);
    public readonly linkedCount = input(0);
    public readonly totalCount = input(0);

    // Show only key vitamins and minerals that have DRI values
    private static readonly KEY_NUTRIENT_IDS = new Set<number>(DAILY_MICRONUTRIENT_IDS);

    public readonly keyNutrients = computed(() =>
        this.nutrients()
            .filter(n => DailyMicronutrientCardComponent.KEY_NUTRIENT_IDS.has(n.nutrientId))
            .map(nutrient => this.toView(nutrient))
            .sort((a, b) => a.name.localeCompare(b.name)),
    );

    private toView(nutrient: DailyMicronutrient): DailyMicronutrientView {
        return {
            ...nutrient,
            percentDailyValueWidth:
                nutrient.percentDailyValue === null ? null : Math.min(Math.max(nutrient.percentDailyValue, 0), PERCENT_MULTIPLIER),
        };
    }
}
