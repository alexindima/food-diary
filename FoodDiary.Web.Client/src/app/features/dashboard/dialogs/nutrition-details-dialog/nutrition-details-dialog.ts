import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiProgressRingComponent } from 'fd-ui-kit/progress-ring/fd-ui-progress-ring';
import { FdUiSectionStateComponent } from 'fd-ui-kit/section-state/fd-ui-section-state';
import { merge, startWith } from 'rxjs';

import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import { normalizeMealType } from '../../../../shared/lib/meal-type.util';
import type { Meal } from '../../../../shared/models/meal.data';
import { NutritionDetailsFacade } from '../../lib/nutrition-details.facade';
import { buildNutritionSources, nutritionMetricPresentation } from '../../lib/nutrition-details.mapper';
import type { NutritionDetailsAction, NutritionDetailsData } from '../../models/nutrition-details.types';
import { NutritionDetailsSourcesComponent } from './nutrition-details-sources';

const PERCENT = 100;

@Component({
    selector: 'fd-nutrition-details-dialog',
    imports: [
        TranslatePipe,
        LocalizedNumberPipe,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiDialogHeaderDirective,
        FdUiProgressRingComponent,
        FdUiSectionStateComponent,
        NutritionDetailsSourcesComponent,
    ],
    providers: [NutritionDetailsFacade],
    templateUrl: './nutrition-details-dialog.html',
    styleUrl: './nutrition-details-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionDetailsDialogComponent {
    protected readonly facade = inject(NutritionDetailsFacade);
    protected readonly data = inject<NutritionDetailsData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<NutritionDetailsDialogComponent, NutritionDetailsAction>>(FdUiDialogRef);
    private readonly translate = inject(TranslateService);
    private readonly translationChange = toSignal(
        merge(this.translate.onLangChange, this.translate.onTranslationChange).pipe(startWith(null)),
    );
    protected readonly language = computed(() => {
        this.translationChange();
        return resolveTranslateLanguage(this.translate);
    });
    protected readonly metric = nutritionMetricPresentation(this.data.metric);
    protected readonly dateLabel = computed(() =>
        new Intl.DateTimeFormat(resolveAppLocale(this.language()), { day: 'numeric', month: 'long', year: 'numeric' }).format(
            this.data.date,
        ),
    );
    protected readonly state = computed(() => (this.facade.loading() ? 'loading' : this.facade.failed() ? 'error' : 'content'));
    protected readonly progress = computed(() => {
        const progress = this.facade.progress();
        if (progress === null) {
            return null;
        }
        const hasGoal = progress.goal !== null && progress.goal > 0;
        return {
            ...progress,
            hasGoal,
            goal: hasGoal ? (progress.goal ?? 0) : 0,
            percent: hasGoal ? Math.round((progress.current / (progress.goal ?? 1)) * PERCENT) : 0,
            difference: Math.abs((progress.goal ?? 0) - progress.current),
            balanceKey: progress.current > (progress.goal ?? 0) ? 'DASHBOARD.DAY_SUMMARY.EXCEEDED_BY' : 'DASHBOARD.DAY_SUMMARY.REMAINING',
        };
    });
    protected readonly sources = computed(() => {
        this.translationChange();
        const day = this.facade.day();
        const total = this.progress()?.current ?? 0;
        return buildNutritionSources(day?.meals ?? [], this.data.metric, this.language()).map(source => {
            const mealType = normalizeMealType(source.meal.mealType) ?? 'OTHER';
            const mealLabel: string = this.translate.instant(`MEAL_CARD.MEAL_TYPES.${mealType}`);
            const time = new Intl.DateTimeFormat(resolveAppLocale(this.language()), { hour: '2-digit', minute: '2-digit' }).format(
                new Date(source.meal.date),
            );
            return {
                ...source,
                title: source.name !== null && source.name.length > 0 ? source.name : mealLabel,
                subtitle: `${mealLabel} · ${time}`,
                share: total > 0 ? Math.min(PERCENT, (source.value / total) * PERCENT) : 0,
            };
        });
    });
    protected openMeal(meal: Meal): void {
        this.dialogRef.close({ kind: 'meal', meal });
    }
    protected close(): void {
        this.dialogRef.close();
    }
    protected openGoals(): void {
        this.dialogRef.close({ kind: 'goals' });
    }
    protected openDiary(): void {
        this.dialogRef.close({ kind: 'diary' });
    }
}
