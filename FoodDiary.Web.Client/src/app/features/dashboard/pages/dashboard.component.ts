import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { NavigationService } from '../../../services/navigation.service';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiDatepicker, FdUiDatepickerInputEvent, FdUiDatepickerModule } from 'fd-ui-kit/material';
import { FdUiInputFieldModule } from 'fd-ui-kit/material';
import { FdUiFormFieldModule } from 'fd-ui-kit/material';
import { FdUiNativeDateModule } from 'fd-ui-kit/material';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { LocalizedDatePipe } from '../../../pipes/localized-date.pipe';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { CalorieGoalDialogComponent } from '../../goals/dialogs/calorie-goal-dialog/calorie-goal-dialog.component';
import { DashboardSummaryCardComponent } from '../../../components/shared/dashboard-summary-card/dashboard-summary-card.component';
import { HydrationCardComponent } from '../components/hydration-card/hydration-card.component';
import { WeightTrendCardComponent } from '../components/weight-trend-card/weight-trend-card.component';
import { DailyAdviceCardComponent } from '../components/daily-advice-card/daily-advice-card.component';
import { MealsPreviewComponent } from '../../../components/shared/meals-preview/meals-preview.component';
import { CycleSummaryCardComponent } from '../components/cycle-summary-card/cycle-summary-card.component';
import { TdeeInsightCardComponent } from '../components/tdee-insight-card/tdee-insight-card.component';
import { NoticeBannerComponent } from '../../../components/shared/notice-banner/notice-banner.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { UnsavedChangesService, UnsavedChangesHandler } from '../../../services/unsaved-changes.service';
import { DashboardLayoutService } from '../lib/dashboard-layout.service';
import { DashboardFacade } from '../lib/dashboard.facade';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

@Component({
    selector: 'fd-dashboard',
    standalone: true,
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDatepickerModule,
        FdUiInputFieldModule,
        FdUiFormFieldModule,
        FdUiNativeDateModule,
        FdUiIconModule,
        PageBodyComponent,
        FdPageContainerDirective,
        LocalizedDatePipe,
        DashboardSummaryCardComponent,
        HydrationCardComponent,
        WeightTrendCardComponent,
        DailyAdviceCardComponent,
        CycleSummaryCardComponent,
        TdeeInsightCardComponent,
        MealsPreviewComponent,
        NoticeBannerComponent,
        FdUiLoaderComponent,
    ],
    providers: [DashboardLayoutService, DashboardFacade, provideCharts(withDefaultRegisterables())],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly facade = inject(DashboardFacade);
    public readonly layout = inject(DashboardLayoutService);

    private readonly headerDatePicker = viewChild<FdUiDatepicker<Date>>('headerDatePicker');

    public readonly selectedDate = this.facade.selectedDate;
    public readonly isTodaySelected = this.facade.isTodaySelected;
    public readonly snapshot = this.facade.snapshot;
    public readonly isLoading = this.facade.isLoading;
    public readonly dailyGoal = this.facade.dailyGoal;
    public readonly todayCalories = this.facade.todayCalories;
    public readonly caloriesBurned = this.facade.caloriesBurned;
    public readonly meals = this.facade.meals;
    public readonly latestWeight = this.facade.latestWeight;
    public readonly previousWeight = this.facade.previousWeight;
    public readonly desiredWeight = this.facade.desiredWeight;
    public readonly latestWaist = this.facade.latestWaist;
    public readonly previousWaist = this.facade.previousWaist;
    public readonly desiredWaist = this.facade.desiredWaist;
    public readonly weeklyConsumed = this.facade.weeklyConsumed;
    public readonly hydration = this.facade.hydration;
    public readonly dailyAdvice = this.facade.dailyAdvice;
    public readonly isHydrationLoading = this.facade.isHydrationLoading;
    public readonly isWeightTrendLoading = this.facade.isWeightTrendLoading;
    public readonly isWaistTrendLoading = this.facade.isWaistTrendLoading;
    public readonly isAdviceLoading = this.facade.isAdviceLoading;
    public readonly cycle = this.facade.cycle;
    public readonly isCycleLoading = this.facade.isCycleLoading;
    public readonly tdeeInsight = this.facade.tdeeInsight;
    public readonly isTdeeLoading = this.facade.isTdeeLoading;
    public readonly weightTrend = this.facade.weightTrend;
    public readonly waistTrend = this.facade.waistTrend;
    public readonly nutrientBars = this.facade.nutrientBars;
    public readonly consumptionRingData = this.facade.consumptionRingData;
    public readonly mealPreviewEntries = this.facade.mealPreviewEntries;
    public readonly placeholderIcon = this.facade.placeholderIcon;
    public readonly placeholderLabel = this.facade.placeholderLabel;

    public ngOnInit(): void {
        this.facade.initialize();
        const handler: UnsavedChangesHandler = {
            hasChanges: () => this.layout.hasLayoutChanges(),
            save: () => this.layout.save(),
            discard: () => this.layout.discard(),
        };
        this.unsavedChangesService.register(handler);
        this.destroyRef.onDestroy(() => this.unsavedChangesService.clear(handler));
    }

    public openDatePicker(): void {
        this.headerDatePicker()?.open();
    }

    public handleDateChange(event: FdUiDatepickerInputEvent<Date>): void {
        if (!event.value) {
            return;
        }
        this.facade.setSelectedDate(event.value);
    }

    public async openWeightHistory(): Promise<void> {
        await this.navigationService.navigateToWeightHistory();
    }

    public async openWaistHistory(): Promise<void> {
        await this.navigationService.navigateToWaistHistory();
    }

    public async openCycleTracking(): Promise<void> {
        await this.navigationService.navigateToCycleTracking();
    }

    public async openGoals(): Promise<void> {
        await this.navigationService.navigateToGoals();
    }

    public openCalorieGoalDialog(): void {
        this.dialogService
            .open(CalorieGoalDialogComponent, {
                size: 'sm',
                data: {
                    dailyCalorieTarget: this.dailyGoal() || null,
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(saved => {
                if (saved) {
                    this.facade.reload();
                }
            });
    }

    public async addConsumption(mealType?: string | null): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd(mealType ?? undefined);
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public openConsumption(consumption: { id: string }): void {
        void this.navigationService.navigateToConsumptionEdit(consumption.id);
    }

    public addHydration(amount: number): void {
        this.facade.addHydration(amount);
    }

    public applyTdeeGoal(target: number): void {
        this.facade.applyTdeeGoal(target);
    }
}
