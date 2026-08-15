import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective, FdUiIconComponent, FdUiWeekPickerComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, map } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { WeeklyGoalDialogComponent, type WeeklyGoalDialogData } from '../../dialogs/weekly-goal-dialog/weekly-goal-dialog';
import { WeeklyReviewDialogComponent, type WeeklyReviewDialogData } from '../../dialogs/weekly-review-dialog/weekly-review-dialog';
import { WeeklyCheckInFacade } from '../../lib/weekly-check-in.facade';
import type { WeeklyReviewViewModel } from '../../lib/weekly-check-in.types';
import type { WeeklyGoal } from '../../models/weekly-goal.data';
import { WEEKLY_CHECK_IN_TOUR } from './weekly-check-in-tour';

@Component({
    selector: 'fd-weekly-check-in-page',
    imports: [
        TranslatePipe,
        MeasurementUnitPipe,
        MeasurementValuePipe,
        DecimalPipe,
        FdUiHintDirective,
        FdUiIconComponent,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiWeekPickerComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
    ],
    templateUrl: './weekly-check-in-page.html',
    styleUrl: './weekly-check-in-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WeeklyCheckInFacade],
})
export class WeeklyCheckInPageComponent {
    private static readonly SUMMARY_INSIGHT_LIMIT = 3;

    private readonly facade = inject(WeeklyCheckInFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly translateService = inject(TranslateService);
    protected readonly measurements = inject(MeasurementSystemService);

    protected readonly isLoading = this.facade.isLoading;
    protected readonly isRefreshing = this.facade.isRefreshing;
    protected readonly thisWeek = this.facade.thisWeek;
    protected readonly review = this.facade.review;
    protected readonly summaryInsights = computed(
        () => this.review()?.insights.slice(0, WeeklyCheckInPageComponent.SUMMARY_INSIGHT_LIMIT) ?? [],
    );
    protected readonly remainingInsightCount = computed(() =>
        Math.max((this.review()?.insights.length ?? 0) - WeeklyCheckInPageComponent.SUMMARY_INSIGHT_LIMIT, 0),
    );
    protected readonly weeklyGoal = this.facade.weeklyGoal;
    protected readonly selectedWeekGoal = this.facade.selectedWeekGoal;
    protected readonly isGoalLoading = this.facade.isGoalLoading;
    protected readonly isSelectedWeekGoalLoading = this.facade.isSelectedWeekGoalLoading;
    protected readonly selectedWeek = this.facade.selectedWeek;
    protected readonly isSelectedWeekPast = this.facade.isSelectedWeekPast;
    protected readonly isGoalPeriodClosed = this.facade.isGoalPeriodClosed;
    protected readonly maximumWeek = new Date();
    protected readonly language = toSignal(this.translateService.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translateService),
    });

    public constructor() {
        this.facade.initialize();
    }

    protected startWeeklyCheckInTour(force = true): void {
        this.tourService.start(this.localizedTour.build(WEEKLY_CHECK_IN_TOUR), { force });
    }

    protected openGoalDialog(): void {
        if (this.isGoalPeriodClosed()) {
            return;
        }

        void this.openGoalDialogAsync(this.facade.goalWeekStartIso(), this.weeklyGoal(), 'WEEKLY_CHECK_IN.GOAL.DIALOG_TITLE');
    }

    protected openSelectedWeekGoalDialog(): void {
        if (this.isSelectedWeekPast()) {
            return;
        }

        void this.openGoalDialogAsync(
            this.facade.selectedWeekStartIso(),
            this.selectedWeekGoal(),
            'WEEKLY_CHECK_IN.GOAL.CURRENT_DIALOG_TITLE',
        );
    }

    protected openDetailedReport(review: WeeklyReviewViewModel): void {
        const week = this.thisWeek();
        if (week === undefined) {
            return;
        }

        this.dialogService.open<WeeklyReviewDialogComponent, WeeklyReviewDialogData>(WeeklyReviewDialogComponent, {
            preset: 'detail',
            data: { review, week },
        });
    }

    private async openGoalDialogAsync(weekStart: string, goal: WeeklyGoal | null, titleKey: string): Promise<void> {
        const dialogRef = this.dialogService.open<WeeklyGoalDialogComponent, WeeklyGoalDialogData, WeeklyGoal | null>(
            WeeklyGoalDialogComponent,
            {
                preset: 'form',
                data: {
                    weekStart,
                    titleKey,
                    goal,
                    saveGoalAsync: async payload => this.facade.saveGoalAsync(payload),
                },
            },
        );
        const result = await firstValueFrom(dialogRef.afterClosed());
        if (result !== null && result !== undefined) {
            this.facade.reloadGoal();
        }
    }
}
