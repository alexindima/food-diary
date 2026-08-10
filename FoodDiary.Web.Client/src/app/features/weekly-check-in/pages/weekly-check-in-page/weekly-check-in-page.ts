import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective, FdUiIconComponent, FdUiWeekPickerComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { map } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { WeeklyReviewDialogComponent, type WeeklyReviewDialogData } from '../../dialogs/weekly-review-dialog/weekly-review-dialog';
import { WeeklyCheckInFacade } from '../../lib/weekly-check-in.facade';
import type { WeeklyReviewViewModel } from '../../lib/weekly-check-in.types';
import { WEEKLY_CHECK_IN_TOUR } from './weekly-check-in-tour';

@Component({
    selector: 'fd-weekly-check-in-page',
    imports: [
        TranslatePipe,
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
    protected readonly focusAccepted = signal(false);
    protected readonly selectedWeek = this.facade.selectedWeek;
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

    protected acceptFocus(): void {
        this.focusAccepted.set(true);
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
}
