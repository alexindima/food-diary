import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { DailyMicronutrient } from '../../models/usda.data';

type DailyMicronutrientView = DailyMicronutrient & {
    percentDailyValueWidth: number | null;
};

const NUTRIENT_ID_VITAMIN_A = 1106;
const NUTRIENT_ID_VITAMIN_C = 1162;
const NUTRIENT_ID_VITAMIN_D = 1110;
const NUTRIENT_ID_VITAMIN_E = 1109;
const NUTRIENT_ID_VITAMIN_B6 = 1175;
const NUTRIENT_ID_VITAMIN_B12 = 1178;
const NUTRIENT_ID_CALCIUM = 1087;
const NUTRIENT_ID_IRON = 1089;
const NUTRIENT_ID_MAGNESIUM = 1090;
const NUTRIENT_ID_POTASSIUM = 1092;
const NUTRIENT_ID_ZINC = 1095;
const NUTRIENT_ID_SELENIUM = 1103;
const MAX_PERCENT_DAILY_VALUE = 100;

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
    private static readonly KEY_NUTRIENT_IDS = new Set([
        NUTRIENT_ID_VITAMIN_A,
        NUTRIENT_ID_VITAMIN_C,
        NUTRIENT_ID_VITAMIN_D,
        NUTRIENT_ID_VITAMIN_E,
        NUTRIENT_ID_VITAMIN_B6,
        NUTRIENT_ID_VITAMIN_B12,
        NUTRIENT_ID_CALCIUM,
        NUTRIENT_ID_IRON,
        NUTRIENT_ID_MAGNESIUM,
        NUTRIENT_ID_POTASSIUM,
        NUTRIENT_ID_ZINC,
        NUTRIENT_ID_SELENIUM,
    ]);

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
                nutrient.percentDailyValue === null ? null : Math.min(Math.max(nutrient.percentDailyValue, 0), MAX_PERCENT_DAILY_VALUE),
        };
    }
}
