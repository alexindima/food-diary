import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { MINERAL_NUTRIENT_IDS, VITAMIN_NUTRIENT_IDS } from '../../lib/usda-nutrient.constants';
import type { Micronutrient } from '../../models/usda.data';
import type { MicronutrientView } from './micronutrient-panel.types';
import { MicronutrientSectionComponent } from './micronutrient-section.component';

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

    private static readonly VITAMIN_IDS = new Set<number>(VITAMIN_NUTRIENT_IDS);
    private static readonly MINERAL_IDS = new Set<number>(MINERAL_NUTRIENT_IDS);

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
                nutrient.percentDailyValue === null ? null : Math.min(Math.max(nutrient.percentDailyValue, 0), PERCENT_MULTIPLIER),
        };
    }
}
