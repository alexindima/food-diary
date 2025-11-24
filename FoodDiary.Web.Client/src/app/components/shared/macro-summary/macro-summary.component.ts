import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardActionsDirective } from 'fd-ui-kit/card/fd-ui-card-actions.directive';
import { NutrientData } from '../../../types/charts.data';
import { CHART_COLORS } from '../../../constants/chart-colors';

@Component({
    selector: 'fd-macro-summary',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiAccentSurfaceComponent,
        FdUiButtonComponent,
        FdUiCardActionsDirective,
    ],
    templateUrl: './macro-summary.component.html',
    styleUrl: './macro-summary.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MacroSummaryComponent {
    public readonly nutrientData = input.required<NutrientData>();
    public readonly fiber = input<number>(0);
    public readonly proteinGoal = input<number | null>(null);
    public readonly fatGoal = input<number | null>(null);
    public readonly carbGoal = input<number | null>(null);
    public readonly fiberGoal = input<number | null>(null);
    public readonly settingsClick = output<void>();

    public readonly items = computed<MacroItem[]>(() => {
        const data = this.nutrientData();
        return [
            {
                labelKey: 'MACRO_SUMMARY.PROTEINS',
                value: data.proteins ?? 0,
                unitKey: 'MACRO_SUMMARY.UNIT',
                color: CHART_COLORS.proteins,
                goal: this.proteinGoal(),
            },
            {
                labelKey: 'MACRO_SUMMARY.FATS',
                value: data.fats ?? 0,
                unitKey: 'MACRO_SUMMARY.UNIT',
                color: CHART_COLORS.fats,
                goal: this.fatGoal(),
            },
            {
                labelKey: 'MACRO_SUMMARY.CARBS',
                value: data.carbs ?? 0,
                unitKey: 'MACRO_SUMMARY.UNIT',
                color: CHART_COLORS.carbs,
                goal: this.carbGoal(),
            },
            {
                labelKey: 'MACRO_SUMMARY.FIBER',
                value: this.fiber() ?? 0,
                unitKey: 'MACRO_SUMMARY.UNIT',
                color: CHART_COLORS.fiber,
                goal: this.fiberGoal(),
            },
        ];
    });

    public getValueClass(item: MacroItem): string {
        if (!item.goal || item.goal <= 0) {
            return '';
        }

        const progress = (item.value / item.goal) * 100;
        if (progress < 70) {
            return 'macro-summary__value--warn';
        }
        if (progress > 120) {
            return 'macro-summary__value--danger';
        }
        return 'macro-summary__value--ok';
    }
}

type MacroItem = {
    labelKey: string;
    value: number;
    unitKey: string;
    color: string;
    goal: number | null | undefined;
};
