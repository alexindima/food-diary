import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { type DailyMicronutrient } from '../../models/usda.data';

@Component({
    selector: 'fd-daily-micronutrient-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './daily-micronutrient-card.component.html',
    styleUrls: ['./daily-micronutrient-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailyMicronutrientCardComponent {
    protected readonly Math = Math;

    public readonly nutrients = input<DailyMicronutrient[]>([]);
    public readonly linkedCount = input(0);
    public readonly totalCount = input(0);

    // Show only key vitamins and minerals that have DRI values
    private static readonly KEY_NUTRIENT_IDS = new Set([
        1106,
        1162,
        1110,
        1109,
        1175,
        1178, // Vitamins A, C, D, E, B6, B12
        1087,
        1089,
        1090,
        1092,
        1095,
        1103, // Calcium, Iron, Magnesium, Potassium, Zinc, Selenium
    ]);

    public readonly keyNutrients = computed(() =>
        this.nutrients()
            .filter(n => DailyMicronutrientCardComponent.KEY_NUTRIENT_IDS.has(n.nutrientId))
            .sort((a, b) => a.name.localeCompare(b.name)),
    );
}
