
import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    OnInit,
    computed,
    inject,
    signal,
    viewChild
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { TranslateService } from '@ngx-translate/core';
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
import { DashboardService } from '../api/dashboard.service';
import { DashboardSnapshot } from '../models/dashboard.data';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { CalorieGoalDialogComponent } from '../../goals/dialogs/calorie-goal-dialog/calorie-goal-dialog.component';
import {
    DashboardSummaryCardComponent,
} from '../../../components/shared/dashboard-summary-card/dashboard-summary-card.component';
import { HydrationService } from '../../hydration/api/hydration.service';
import { HydrationCardComponent } from '../components/hydration-card/hydration-card.component';
import { WeightTrendCardComponent } from '../components/weight-trend-card/weight-trend-card.component';
import { DailyAdviceCardComponent } from '../components/daily-advice-card/daily-advice-card.component';
import { MealsPreviewComponent } from '../../../components/shared/meals-preview/meals-preview.component';
import { CycleSummaryCardComponent } from '../components/cycle-summary-card/cycle-summary-card.component';
import { Meal } from '../../meals/models/meal.data';
import { CyclesService } from '../../cycle-tracking/api/cycles.service';
import { CycleResponse } from '../../cycle-tracking/models/cycle.data';
import { NoticeBannerComponent } from '../../../components/shared/notice-banner/notice-banner.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { auditTime, fromEvent } from 'rxjs';
import { UnsavedChangesService, UnsavedChangesHandler } from '../../../services/unsaved-changes.service';
import { DashboardLayoutService } from '../lib/dashboard-layout.service';
import { normalizeDate, getDashboardDateUtc, getHydrationDateUtc } from '../lib/dashboard-date.utils';
import { createWeightTrendSignals, createWaistTrendSignals } from '../lib/dashboard-trend.utils';
import {
    createNutrientBarsSignal,
    createConsumptionRingSignal,
    createMealPreviewSignal,
    placeholderIcon,
    placeholderLabel,
} from '../lib/dashboard-nutrition.utils';

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
    MealsPreviewComponent,
    NoticeBannerComponent,
    FdUiLoaderComponent
],
    providers: [DashboardLayoutService],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dashboardService = inject(DashboardService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly hydrationService = inject(HydrationService);
    private readonly cyclesService = inject(CyclesService);
    private readonly translateService = inject(TranslateService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    public readonly layout = inject(DashboardLayoutService);

    private readonly headerDatePicker = viewChild<FdUiDatepicker<Date>>('headerDatePicker');

    public selectedDate = signal<Date>(normalizeDate(new Date()));
    public readonly isTodaySelected = computed(() => {
        const today = normalizeDate(new Date());
        return this.selectedDate().getTime() === today.getTime();
    });
    public snapshot = signal<DashboardSnapshot | null>(null);
    public isLoading = signal<boolean>(false);

    public readonly dailyGoal = computed(() => this.snapshot()?.dailyGoal ?? 0);
    public readonly todayCalories = computed(() => this.snapshot()?.statistics.totalCalories ?? 0);
    public readonly meals = computed<Meal[]>(() => this.snapshot()?.meals.items ?? []);
    public readonly latestWeight = computed(() => this.snapshot()?.weight.latest?.weight ?? null);
    public readonly previousWeight = computed(() => this.snapshot()?.weight.previous?.weight ?? null);
    public readonly desiredWeight = computed(() => this.snapshot()?.weight.desired ?? null);
    public readonly latestWaist = computed(() => this.snapshot()?.waist.latest?.circumference ?? null);
    public readonly previousWaist = computed(() => this.snapshot()?.waist.previous?.circumference ?? null);
    public readonly desiredWaist = computed(() => this.snapshot()?.waist.desired ?? null);
    public readonly weeklyConsumed = computed(() =>
        (this.snapshot()?.weeklyCalories ?? []).reduce((sum, point) => sum + (point?.calories ?? 0), 0)
    );
    private readonly isHydrationUpdating = signal<boolean>(false);
    private readonly trendDays = 7;
    public readonly hydration = computed(() => this.snapshot()?.hydration ?? null);
    public readonly dailyAdvice = computed(() => this.snapshot()?.advice ?? null);
    private readonly weightTrendPoints = computed(() => this.snapshot()?.weightTrend ?? []);
    private readonly waistTrendPoints = computed(() => this.snapshot()?.waistTrend ?? []);
    public readonly isHydrationLoading = computed(() => this.isLoading() || this.isHydrationUpdating());
    public readonly isWeightTrendLoading = computed(() => this.isLoading());
    public readonly isWaistTrendLoading = computed(() => this.isLoading());
    public readonly isAdviceLoading = computed(() => this.isLoading());
    public readonly cycle = signal<CycleResponse | null>(null);
    public readonly isCycleLoading = signal<boolean>(false);

    // Trend signals
    public readonly weightTrend = createWeightTrendSignals(
        this.weightTrendPoints, this.latestWeight, this.selectedDate, this.trendDays,
    );
    public readonly waistTrend = createWaistTrendSignals(
        this.waistTrendPoints, this.latestWaist, this.selectedDate, this.trendDays,
    );

    // Nutrition signals
    public readonly nutrientBars = createNutrientBarsSignal(this.snapshot);
    public readonly consumptionRingData = createConsumptionRingSignal(
        this.snapshot, this.weeklyConsumed, this.nutrientBars,
    );
    public readonly mealPreviewEntries = createMealPreviewSignal(this.meals, this.isTodaySelected);

    public readonly placeholderIcon = placeholderIcon;
    public readonly placeholderLabel = placeholderLabel;

    public ngOnInit(): void {
        this.loadDashboardSnapshot();
        this.loadCycle();
        const handler: UnsavedChangesHandler = {
            hasChanges: () => this.layout.hasLayoutChanges(),
            save: () => this.layout.save(),
            discard: () => this.layout.discard(),
        };
        this.unsavedChangesService.register(handler);
        this.destroyRef.onDestroy(() => this.unsavedChangesService.clear(handler));

        if (typeof window !== 'undefined') {
            fromEvent(window, 'resize')
                .pipe(auditTime(150), takeUntilDestroyed(this.destroyRef))
                .subscribe(() => this.layout.updateViewportWidth(window.innerWidth));
        }

        this.translateService.onLangChange
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.loadDashboardSnapshot(false));
    }

    public openDatePicker(): void {
        this.headerDatePicker()?.open();
    }

    public handleDateChange(event: FdUiDatepickerInputEvent<Date>): void {
        if (!event.value) {
            return;
        }

        const normalized = normalizeDate(event.value);

        if (normalized.getTime() !== this.selectedDate().getTime()) {
            this.selectedDate.set(normalized);
            this.loadDashboardSnapshot();
        }
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
                    this.loadDashboardSnapshot();
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
        this.isHydrationUpdating.set(true);
        const targetDate = getHydrationDateUtc(this.selectedDate());
        this.hydrationService
            .addEntry(amount, targetDate)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => this.loadDashboardSnapshot(false, true),
                error: () => this.isHydrationUpdating.set(false),
            });
    }

    private loadDashboardSnapshot(showLoader = true, clearHydrationUpdate = false): void {
        const targetDate = getDashboardDateUtc(this.selectedDate());
        const locale = this.getCurrentLocale();

        if (showLoader) {
            this.isLoading.set(true);
        }

        this.dashboardService
            .getSnapshot(targetDate, 1, 10, locale, this.trendDays)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: snapshot => {
                    this.snapshot.set(snapshot);
                    this.layout.initializeLayout(snapshot?.dashboardLayout ?? null);
                    this.isLoading.set(false);
                    if (clearHydrationUpdate) {
                        this.isHydrationUpdating.set(false);
                    }
                },
                error: () => {
                    this.snapshot.set(null);
                    this.layout.initializeLayout(null);
                    this.isLoading.set(false);
                    if (clearHydrationUpdate) {
                        this.isHydrationUpdating.set(false);
                    }
                },
            });
    }

    private getCurrentLocale(): string {
        const lang = this.translateService.currentLang || this.translateService.getDefaultLang() || 'en';
        return lang.split(/[-_]/)[0];
    }

    private loadCycle(): void {
        this.isCycleLoading.set(true);
        this.cyclesService
            .getCurrent()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: cycle => {
                    this.cycle.set(cycle);
                    this.isCycleLoading.set(false);
                },
                error: () => {
                    this.cycle.set(null);
                    this.isCycleLoading.set(false);
                },
            });
    }
}
