import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { NutrientData } from '../../../types/charts.data';
import { CHART_COLORS } from '../../../constants/chart-colors';

@Component({
    selector: 'fd-macro-summary',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiCardComponent, FdUiAccentSurfaceComponent],
    templateUrl: './macro-summary.component.html',
    styleUrl: './macro-summary.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MacroSummaryComponent {
    public readonly nutrientData = input.required<NutrientData>();
    public readonly fiber = input<number>(0);

    public readonly items = computed(() => {
        const data = this.nutrientData();
        return [
            {
                labelKey: 'MACRO_SUMMARY.PROTEINS',
                value: data.proteins ?? 0,
                unitKey: 'MACRO_SUMMARY.UNIT',
                color: CHART_COLORS.proteins,
            },
            {
                labelKey: 'MACRO_SUMMARY.FATS',
                value: data.fats ?? 0,
                unitKey: 'MACRO_SUMMARY.UNIT',
                color: CHART_COLORS.fats,
            },
            {
                labelKey: 'MACRO_SUMMARY.CARBS',
                value: data.carbs ?? 0,
                unitKey: 'MACRO_SUMMARY.UNIT',
                color: CHART_COLORS.carbs,
            },
            {
                labelKey: 'MACRO_SUMMARY.FIBER',
                value: this.fiber() ?? 0,
                unitKey: 'MACRO_SUMMARY.UNIT',
                color: CHART_COLORS.fiber,
            },
        ];
    });
}
