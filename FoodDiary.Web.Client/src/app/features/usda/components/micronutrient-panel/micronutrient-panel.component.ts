import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Micronutrient } from '../../models/usda.data';

@Component({
    selector: 'fd-micronutrient-panel',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './micronutrient-panel.component.html',
    styleUrls: ['./micronutrient-panel.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MicronutrientPanelComponent {
    protected readonly Math = Math;

    public readonly nutrients = input<Micronutrient[]>([]);

    private static readonly VITAMIN_IDS = new Set([1106, 1165, 1166, 1167, 1170, 1175, 1177, 1178, 1162, 1110, 1109, 1185]);
    private static readonly MINERAL_IDS = new Set([1087, 1089, 1090, 1091, 1092, 1093, 1095, 1098, 1101, 1103]);

    public readonly vitamins = computed(() => this.nutrients().filter(n => MicronutrientPanelComponent.VITAMIN_IDS.has(n.nutrientId)));

    public readonly minerals = computed(() => this.nutrients().filter(n => MicronutrientPanelComponent.MINERAL_IDS.has(n.nutrientId)));
}
