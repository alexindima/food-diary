import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { DailyMicronutrientSummary, DailyMicronutrient, HealthAreaScores } from '../../../usda/models/usda.data';
import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';

@Component({
    selector: 'fd-micronutrient-card',
    standalone: true,
    imports: [DecimalPipe, TranslatePipe, FdCardHoverDirective],
    templateUrl: './micronutrient-card.component.html',
    styleUrl: './micronutrient-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MicronutrientCardComponent {
    public readonly summary = input<DailyMicronutrientSummary | null>(null);
    public readonly isLoading = input<boolean>(false);

    public readonly topDeficiencies = computed<DailyMicronutrient[]>(() => {
        const data = this.summary();
        if (!data?.nutrients.length) {
            return [];
        }

        return data.nutrients
            .filter(n => n.percentDailyValue !== null && n.percentDailyValue < 100)
            .sort((a, b) => (a.percentDailyValue ?? 0) - (b.percentDailyValue ?? 0))
            .slice(0, 5);
    });

    public readonly topSufficient = computed<DailyMicronutrient[]>(() => {
        const data = this.summary();
        if (!data?.nutrients.length) {
            return [];
        }

        return data.nutrients
            .filter(n => n.percentDailyValue !== null && n.percentDailyValue >= 100)
            .sort((a, b) => (b.percentDailyValue ?? 0) - (a.percentDailyValue ?? 0))
            .slice(0, 3);
    });

    public readonly healthScores = computed<HealthAreaScores | null>(() => this.summary()?.healthScores ?? null);

    public readonly coverage = computed(() => {
        const data = this.summary();
        if (!data) {
            return null;
        }
        return { linked: data.linkedProductCount, total: data.totalProductCount };
    });

    public readonly hasData = computed(() => {
        const data = this.summary();
        return !!data && data.nutrients.length > 0;
    });

    public getBarWidth(nutrient: DailyMicronutrient): number {
        return Math.min(nutrient.percentDailyValue ?? 0, 100);
    }

    public getBarColor(nutrient: DailyMicronutrient): string {
        const pct = nutrient.percentDailyValue ?? 0;
        if (pct >= 80) {
            return 'var(--fd-color-green-500)';
        }
        if (pct >= 50) {
            return '#eab308';
        }
        return '#ef4444';
    }

    public shortenName(name: string): string {
        return name
            .replace(/, total ascorbic acid/i, '')
            .replace(/, DFE/i, '')
            .replace(/, RAE/i, '')
            .replace(/, Ca$/i, '')
            .replace(/, Fe$/i, '')
            .replace(/, Mg$/i, '')
            .replace(/, Zn$/i, '')
            .replace(/, K$/i, '')
            .replace(/, P$/i, '')
            .replace(/^Vitamin /, 'Vit. ');
    }
}
