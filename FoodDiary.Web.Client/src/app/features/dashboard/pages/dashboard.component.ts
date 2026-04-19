import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, computed, inject, viewChild } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
import { MicronutrientCardComponent } from '../components/micronutrient-card/micronutrient-card.component';
import { NoticeBannerComponent } from '../../../components/shared/notice-banner/notice-banner.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { UnsavedChangesService, UnsavedChangesHandler } from '../../../services/unsaved-changes.service';
import { DashboardLayoutService } from '../lib/dashboard-layout.service';
import { DashboardFacade } from '../lib/dashboard.facade';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { FastingTimerCardComponent } from '../../fasting/components/fasting-timer-card/fasting-timer-card.component';
import { FastingStagePresentation, resolveFastingStage } from '../../fasting/lib/fasting-stage';
import { AiInputBarComponent } from '../../../components/shared/ai-input-bar/ai-input-bar.component';
import { DashboardCardShellComponent } from '../components/dashboard-card-shell/dashboard-card-shell.component';
import {
    formatDashboardFastingDuration,
    getDashboardCyclicPhaseProgressLabel,
    getDashboardFastingCycleLabel,
    getDashboardFastingOccurrenceLabel,
    getDashboardFastingProtocolBaseLabel,
} from '../lib/dashboard-fasting.utils';

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
        MicronutrientCardComponent,
        MealsPreviewComponent,
        NoticeBannerComponent,
        FdUiLoaderComponent,
        FastingTimerCardComponent,
        AiInputBarComponent,
        DashboardCardShellComponent,
    ],
    providers: [DashboardLayoutService, DashboardFacade, provideCharts(withDefaultRegisterables())],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly facade = inject(DashboardFacade);
    private readonly translateService = inject(TranslateService);
    private readonly translate = (key: string, params?: Record<string, unknown>): string => this.translateService.instant(key, params);
    public readonly layout = inject(DashboardLayoutService);

    private readonly headerDatePicker = viewChild<FdUiDatepicker<Date>>('headerDatePicker');
    private readonly dashboardRoot = viewChild.required<ElementRef<HTMLElement>>('dashboardRoot');
    private resizeObserver: ResizeObserver | null = null;

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
    public readonly micronutrients = this.facade.micronutrients;
    public readonly isMicronutrientsLoading = this.facade.isMicronutrientsLoading;
    public readonly weightTrend = this.facade.weightTrend;
    public readonly waistTrend = this.facade.waistTrend;
    public readonly nutrientBars = this.facade.nutrientBars;
    public readonly consumptionRingData = this.facade.consumptionRingData;
    public readonly mealPreviewEntries = this.facade.mealPreviewEntries;
    public readonly placeholderIcon = this.facade.placeholderIcon;
    public readonly placeholderLabel = this.facade.placeholderLabel;
    public readonly fastingIsActive = this.facade.fastingIsActive;
    public readonly fastingCurrentSession = this.facade.currentFastingSession;
    public readonly fastingProgressPercent = computed(() => this.fastingWidgetState().progressPercent);
    public readonly fastingElapsedFormatted = computed(() => this.fastingWidgetState().elapsedFormatted);
    public readonly fastingRemainingFormatted = computed(() => this.fastingWidgetState().remainingFormatted);
    public readonly fastingRemainingLabelKey = computed(() => this.fastingWidgetState().remainingLabelKey);
    public readonly fastingIsOvertime = computed(() => this.fastingWidgetState().isOvertime);
    public readonly fastingStage = computed<FastingStagePresentation | null>(() => this.fastingWidgetState().stage);
    public readonly fastingStageIndex = computed<number | null>(() =>
        this.fastingWidgetState().showStageProgress ? (this.fastingStage()?.index ?? null) : null,
    );
    public readonly fastingTotalStages = computed(() =>
        this.fastingWidgetState().showStageProgress ? (this.fastingStage()?.total ?? 4) : 0,
    );
    public readonly fastingStateLabel = computed(() => this.fastingWidgetState().stateLabel);
    public readonly fastingDetailLabel = computed(() => this.fastingWidgetState().detailLabel);
    public readonly fastingMetaLabel = computed(() => this.fastingWidgetState().metaLabel);
    public readonly fastingRingColor = computed(() => this.fastingWidgetState().ringColor);
    private readonly fastingBaseStage = computed<FastingStagePresentation | null>(() => {
        const session = this.fastingCurrentSession();
        if (!session) {
            return null;
        }

        return resolveFastingStage(this.facade.fastingElapsedMs(), session.plannedDurationHours);
    });
    private readonly fastingWidgetState = computed(() => {
        const session = this.fastingCurrentSession();
        const fallback = {
            progressPercent: this.facade.fastingProgressPercent(),
            elapsedFormatted: this.facade.fastingElapsedFormatted(),
            remainingFormatted: this.facade.fastingRemainingFormatted(),
            remainingLabelKey: 'FASTING.REMAINING',
            isOvertime: this.facade.fastingIsOvertime(),
            showStageProgress: true,
            stateLabel: getDashboardFastingOccurrenceLabel(this.translate, session?.occurrenceKind),
            detailLabel: session ? getDashboardFastingProtocolBaseLabel(this.translate, session) : null,
            metaLabel: session?.planType === 'Cyclic' ? getDashboardCyclicPhaseProgressLabel(this.translate, session) : null,
            ringColor: this.fastingBaseStage()?.color ?? null,
            stage: this.fastingBaseStage(),
        };

        if (!session || session.planType !== 'Intermittent' || session.endedAtUtc) {
            return fallback;
        }

        const fastHours = Math.max(1, session.initialPlannedDurationHours || session.plannedDurationHours);
        const eatingHours = Math.max(1, 24 - fastHours);
        const cycleLengthMs = (fastHours + eatingHours) * 3_600_000;
        const cycleDay = Math.floor(this.facade.fastingElapsedMs() / cycleLengthMs) + 1;
        const cycleElapsedMs = this.facade.fastingElapsedMs() % cycleLengthMs;
        const fastWindowMs = fastHours * 3_600_000;
        const eatingWindowMs = eatingHours * 3_600_000;
        const isFastingWindow = cycleElapsedMs < fastWindowMs;

        if (isFastingWindow) {
            const stage = resolveFastingStage(cycleElapsedMs, fastHours);
            return {
                progressPercent: Math.min((cycleElapsedMs / fastWindowMs) * 100, 100),
                elapsedFormatted: formatDashboardFastingDuration(cycleElapsedMs),
                remainingFormatted: formatDashboardFastingDuration(Math.max(0, fastWindowMs - cycleElapsedMs)),
                remainingLabelKey: 'FASTING.UNTIL_EATING_WINDOW',
                isOvertime: false,
                showStageProgress: true,
                stateLabel: this.translateService.instant('FASTING.FASTING_WINDOW'),
                detailLabel: getDashboardFastingProtocolBaseLabel(this.translate, session),
                metaLabel: getDashboardFastingCycleLabel(this.translate, cycleDay),
                ringColor: stage.color,
                stage,
            };
        }

        const eatingElapsedMs = cycleElapsedMs - fastWindowMs;
        return {
            progressPercent: Math.min((eatingElapsedMs / eatingWindowMs) * 100, 100),
            elapsedFormatted: formatDashboardFastingDuration(eatingElapsedMs),
            remainingFormatted: formatDashboardFastingDuration(Math.max(0, eatingWindowMs - eatingElapsedMs)),
            remainingLabelKey: 'FASTING.NEXT_FAST',
            isOvertime: false,
            showStageProgress: false,
            stateLabel: this.translateService.instant('FASTING.EATING_WINDOW'),
            detailLabel: getDashboardFastingProtocolBaseLabel(this.translate, session),
            metaLabel: getDashboardFastingCycleLabel(this.translate, cycleDay),
            ringColor: 'var(--fd-color-green-500)',
            stage: {
                index: cycleDay,
                total: cycleDay,
                titleKey: 'FASTING.EATING_WINDOW',
                descriptionKey: 'FASTING.EATING_WINDOW_DESCRIPTION',
                color: 'var(--fd-color-green-500)',
                glowColor: 'color-mix(in srgb, var(--fd-color-green-500) 18%, transparent)',
                nextTitleKey: null,
                nextInMs: null,
            } satisfies FastingStagePresentation,
        };
    });
    public readonly fastingNextStageFormatted = computed(() => {
        const stage = this.fastingStage();
        if (!stage?.nextInMs) {
            return null;
        }

        return formatDashboardFastingDuration(stage.nextInMs);
    });
    public readonly shouldRenderFastingWidget = computed(() => {
        if (this.layout.isEditingLayout()) {
            return this.layout.shouldRenderBlock('fasting');
        }

        return this.isTodaySelected() && this.layout.isBlockVisible('fasting') && this.fastingIsActive();
    });
    public readonly hasRenderedAsideBlocks = computed(() => {
        if (this.layout.isEditingLayout()) {
            return this.layout.hasAsideBlocks();
        }

        const visibleBlocks = this.layout.visibleBlocks();
        return visibleBlocks.some(block => ['hydration', 'micronutrients', 'cycle', 'weight', 'waist', 'tdee', 'advice'].includes(block));
    });

    public constructor() {
        this.facade.initialize();
        afterNextRender(() => this.observeDashboardWidth());
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

    public async openNotificationSettings(): Promise<void> {
        await this.navigationService.navigateToProfile();
    }

    public async addConsumption(mealType?: string | null): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd(mealType ?? undefined);
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public onMealCreated(): void {
        this.facade.reload(false);
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

    public handleFastingCardClick(): void {
        if (this.layout.isEditingLayout()) {
            this.layout.toggleBlock('fasting');
            return;
        }

        void this.openFasting();
    }

    public async openFasting(): Promise<void> {
        if (this.layout.isEditingLayout()) {
            return;
        }

        await this.navigationService.navigateToFasting();
    }

    public getFastingCardLabelKey(): string {
        const session = this.fastingCurrentSession();
        if (!session) {
            return 'FASTING.WIDGET_LABEL';
        }

        switch (session.planType) {
            case 'Cyclic':
                return 'FASTING.CYCLIC_TYPE';
            case 'Extended':
                return 'FASTING.EXTENDED_TYPE';
            default:
                return 'FASTING.INTERMITTENT_TYPE';
        }
    }

    public getFastingCardStateLabel(): string | null {
        return this.fastingStateLabel();
    }

    public getFastingCardDetailLabel(): string | null {
        return this.fastingDetailLabel();
    }

    private observeDashboardWidth(): void {
        const element = this.dashboardRoot().nativeElement;

        const updateWidth = (width: number): void => {
            if (width > 0) {
                this.layout.updateViewportWidth(width);
            }
        };

        updateWidth(element.getBoundingClientRect().width);

        if (typeof ResizeObserver === 'undefined') {
            return;
        }

        this.resizeObserver = new ResizeObserver(entries => {
            const entry = entries[0];
            if (!entry) {
                return;
            }

            updateWidth(entry.contentRect.width);
        });

        this.resizeObserver.observe(element);
        this.destroyRef.onDestroy(() => this.resizeObserver?.disconnect());
    }
}
