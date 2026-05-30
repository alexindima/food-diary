import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { buildMineralMicronutrientViews, buildVitaminMicronutrientViews } from '../../lib/usda-micronutrient.mapper';
import type { Micronutrient } from '../../models/usda.data';
import { MicronutrientSectionComponent } from './micronutrient-section/micronutrient-section';

@Component({
    selector: 'fd-micronutrient-panel',
    imports: [TranslatePipe, MicronutrientSectionComponent],
    templateUrl: './micronutrient-panel.html',
    styleUrls: ['./micronutrient-panel.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MicronutrientPanelComponent {
    public readonly nutrients = input<Micronutrient[]>([]);

    protected readonly vitamins = computed(() => buildVitaminMicronutrientViews(this.nutrients()));
    protected readonly minerals = computed(() => buildMineralMicronutrientViews(this.nutrients()));
}
