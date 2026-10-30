import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiEmptyStateComponent } from 'fd-ui-kit/empty-state/fd-ui-empty-state';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { Meal } from '../../../../shared/models/meal.data';
import type { NutritionMetric, NutritionSourceView } from '../../models/nutrition-details.types';

const INITIAL_SOURCE_COUNT = 3;
const SOURCE_KEYS: Record<NutritionMetric, { title: string; share: string }> = {
    calories: { title: 'MEALS', share: 'SHARE_CALORIES' },
    protein: { title: 'SOURCES_PROTEIN', share: 'SHARE_PROTEIN' },
    fats: { title: 'SOURCES_FATS', share: 'SHARE_FATS' },
    carbs: { title: 'SOURCES_CARBS', share: 'SHARE_CARBS' },
    fiber: { title: 'SOURCES_FIBER', share: 'SHARE_FIBER' },
};

@Component({
    selector: 'fd-nutrition-details-sources',
    imports: [TranslatePipe, LocalizedNumberPipe, FdUiButtonComponent, FdUiIconComponent, FdUiEmptyStateComponent],
    templateUrl: './nutrition-details-sources.html',
    styleUrl: './nutrition-details-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionDetailsSourcesComponent {
    public readonly sources = input.required<readonly NutritionSourceView[]>();
    public readonly metric = input.required<NutritionMetric>();
    public readonly unitKey = input.required<string>();
    public readonly language = input.required<string>();
    public readonly mealSelected = output<Meal>();
    protected readonly expanded = signal(false);
    protected readonly titleKey = computed(() => `DASHBOARD.NUTRITION_DETAILS.${SOURCE_KEYS[this.metric()].title}`);
    protected readonly shareKey = computed(() => `DASHBOARD.NUTRITION_DETAILS.${SOURCE_KEYS[this.metric()].share}`);
    protected readonly visibleSources = computed(() => (this.expanded() ? this.sources() : this.sources().slice(0, INITIAL_SOURCE_COUNT)));
    protected readonly hiddenCount = computed(() => this.sources().length - this.visibleSources().length);
    protected readonly hiddenValue = computed(() =>
        this.sources()
            .slice(INITIAL_SOURCE_COUNT)
            .reduce((sum, source) => sum + source.value, 0),
    );
    protected readonly hasUnattributed = computed(() => this.sources().some(source => source.kind === 'unavailable'));
}
