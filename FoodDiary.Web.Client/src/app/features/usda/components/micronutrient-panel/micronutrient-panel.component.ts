import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { Micronutrient } from '../../models/usda.data';
import { MicronutrientSectionComponent } from './micronutrient-section.component';

export type MicronutrientView = Micronutrient & {
    percentDailyValueWidth: number | null;
};

const NUTRIENT_ID_VITAMIN_A = 1106;
const NUTRIENT_ID_THIAMIN = 1165;
const NUTRIENT_ID_RIBOFLAVIN = 1166;
const NUTRIENT_ID_NIACIN = 1167;
const NUTRIENT_ID_PANTOTHENIC_ACID = 1170;
const NUTRIENT_ID_VITAMIN_B6 = 1175;
const NUTRIENT_ID_FOLATE = 1177;
const NUTRIENT_ID_VITAMIN_B12 = 1178;
const NUTRIENT_ID_VITAMIN_C = 1162;
const NUTRIENT_ID_VITAMIN_D = 1110;
const NUTRIENT_ID_VITAMIN_E = 1109;
const NUTRIENT_ID_VITAMIN_K = 1185;
const NUTRIENT_ID_CALCIUM = 1087;
const NUTRIENT_ID_IRON = 1089;
const NUTRIENT_ID_MAGNESIUM = 1090;
const NUTRIENT_ID_PHOSPHORUS = 1091;
const NUTRIENT_ID_POTASSIUM = 1092;
const NUTRIENT_ID_SODIUM = 1093;
const NUTRIENT_ID_ZINC = 1095;
const NUTRIENT_ID_COPPER = 1098;
const NUTRIENT_ID_MANGANESE = 1101;
const NUTRIENT_ID_SELENIUM = 1103;
const MAX_PERCENT_DAILY_VALUE = 100;

@Component({
    selector: 'fd-micronutrient-panel',
    standalone: true,
    imports: [CommonModule, TranslatePipe, MicronutrientSectionComponent],
    templateUrl: './micronutrient-panel.component.html',
    styleUrls: ['./micronutrient-panel.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MicronutrientPanelComponent {
    public readonly nutrients = input<Micronutrient[]>([]);

    private static readonly VITAMIN_IDS = new Set([
        NUTRIENT_ID_VITAMIN_A,
        NUTRIENT_ID_THIAMIN,
        NUTRIENT_ID_RIBOFLAVIN,
        NUTRIENT_ID_NIACIN,
        NUTRIENT_ID_PANTOTHENIC_ACID,
        NUTRIENT_ID_VITAMIN_B6,
        NUTRIENT_ID_FOLATE,
        NUTRIENT_ID_VITAMIN_B12,
        NUTRIENT_ID_VITAMIN_C,
        NUTRIENT_ID_VITAMIN_D,
        NUTRIENT_ID_VITAMIN_E,
        NUTRIENT_ID_VITAMIN_K,
    ]);
    private static readonly MINERAL_IDS = new Set([
        NUTRIENT_ID_CALCIUM,
        NUTRIENT_ID_IRON,
        NUTRIENT_ID_MAGNESIUM,
        NUTRIENT_ID_PHOSPHORUS,
        NUTRIENT_ID_POTASSIUM,
        NUTRIENT_ID_SODIUM,
        NUTRIENT_ID_ZINC,
        NUTRIENT_ID_COPPER,
        NUTRIENT_ID_MANGANESE,
        NUTRIENT_ID_SELENIUM,
    ]);

    public readonly vitamins = computed(() =>
        this.nutrients()
            .filter(n => MicronutrientPanelComponent.VITAMIN_IDS.has(n.nutrientId))
            .map(nutrient => this.toView(nutrient)),
    );

    public readonly minerals = computed(() =>
        this.nutrients()
            .filter(n => MicronutrientPanelComponent.MINERAL_IDS.has(n.nutrientId))
            .map(nutrient => this.toView(nutrient)),
    );

    private toView(nutrient: Micronutrient): MicronutrientView {
        return {
            ...nutrient,
            percentDailyValueWidth:
                nutrient.percentDailyValue === null ? null : Math.min(Math.max(nutrient.percentDailyValue, 0), MAX_PERCENT_DAILY_VALUE),
        };
    }
}
