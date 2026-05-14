import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { buildMineralMicronutrientViews, buildVitaminMicronutrientViews } from '../../lib/usda-micronutrient.mapper';
import type { Micronutrient } from '../../models/usda.data';
import { MicronutrientSectionComponent } from './micronutrient-section/micronutrient-section.component';

@Component({
    selector: 'fd-micronutrient-panel',
    imports: [TranslatePipe, MicronutrientSectionComponent],
    templateUrl: './micronutrient-panel.component.html',
    styleUrls: ['./micronutrient-panel.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MicronutrientPanelComponent {
    public readonly nutrients = input<Micronutrient[]>([]);

    public readonly vitamins = computed(() => buildVitaminMicronutrientViews(this.nutrients()));
    public readonly minerals = computed(() => buildMineralMicronutrientViews(this.nutrients()));
}
