import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { map } from 'rxjs';

import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { type DayCalorieKey, DAYS_OF_WEEK } from '../../models/goals.data';
import { GoalsCyclingDayV2Component } from './goals-cycling-day';

const DAYS_PER_WEEK = 7;
const WEEKDAY_COUNT = 5;
const MINIMUM_BAR_PERCENT = 12;
const AVERAGE_SCALE_MULTIPLIER = 2;

@Component({
    selector: 'fd-goals-cycling-row-v2',
    imports: [TranslatePipe, LocalizedNumberPipe, FdUiButtonComponent, GoalsCyclingDayV2Component],
    templateUrl: './goals-cycling-row.html',
    styleUrl: './goals-page-v2.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsCyclingRowV2Component {
    private readonly translateService = inject(TranslateService);
    public readonly enabled = input.required<boolean>();
    public readonly baseCalories = input.required<number>();
    public readonly dayCalories = input.required<Record<DayCalorieKey, number>>();
    public readonly enabledChange = output<boolean>();
    public readonly dayCaloriesChange = output<{ key: DayCalorieKey; value: number }>();
    protected readonly language = toSignal(this.translateService.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translateService),
    });
    protected readonly total = computed(() => DAYS_OF_WEEK.reduce((sum, day) => sum + this.dayCalories()[day.key], 0));
    protected readonly average = computed(() => Math.round(this.total() / DAYS_PER_WEEK));
    protected readonly days = computed(() => {
        const values = this.dayCalories();
        const maximum = Math.max(this.average() * AVERAGE_SCALE_MULTIPLIER, 1);
        return DAYS_OF_WEEK.map((day, index) => ({
            ...day,
            value: values[day.key],
            barPercent: Math.min(
                PERCENT_MULTIPLIER,
                Math.max(MINIMUM_BAR_PERCENT, Math.round((values[day.key] / maximum) * PERCENT_MULTIPLIER)),
            ),
            weekend: index >= DAYS_PER_WEEK - 2,
            weekendPosition: index === DAYS_PER_WEEK - 2 ? ('start' as const) : index === DAYS_PER_WEEK - 1 ? ('end' as const) : null,
        }));
    });
    protected repeatWeekdays(): void {
        const mondayCalories = this.dayCalories().mondayCalories;
        for (const day of DAYS_OF_WEEK.slice(1, WEEKDAY_COUNT)) {
            this.dayCaloriesChange.emit({ key: day.key, value: mondayCalories });
        }
    }

    protected resetDays(): void {
        for (const day of DAYS_OF_WEEK) {
            this.dayCaloriesChange.emit({ key: day.key, value: this.baseCalories() });
        }
    }
}
