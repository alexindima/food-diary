import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    type ElementRef,
    inject,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDatePickerButtonComponent } from 'fd-ui-kit/date-picker-button/fd-ui-date-picker-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { AiInputBarComponent } from '../../../components/shared/ai-input-bar/ai-input-bar.component';
import { DashboardSummaryCardComponent } from '../../../components/shared/dashboard-summary-card/dashboard-summary-card.component';
import { MealsPreviewComponent } from '../../../components/shared/meals-preview/meals-preview.component';
import { NoticeBannerComponent } from '../../../components/shared/notice-banner/notice-banner.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { LocalizedDatePipe } from '../../../pipes/localized-date.pipe';
import { NavigationService } from '../../../services/navigation.service';
import { ThemeService } from '../../../services/theme.service';
import { type UnsavedChangesHandler, UnsavedChangesService } from '../../../services/unsaved-changes.service';
import { FastingTimerCardComponent } from '../../fasting/components/fasting-timer-card/fasting-timer-card.component';
import { type FastingStagePresentation, resolveFastingStage } from '../../fasting/lib/fasting-stage';
import { CycleSummaryCardComponent } from '../components/cycle-summary-card/cycle-summary-card.component';
import { DailyAdviceCardComponent } from '../components/daily-advice-card/daily-advice-card.component';
import { DashboardCardShellComponent } from '../components/dashboard-card-shell/dashboard-card-shell.component';
import { HydrationCardComponent } from '../components/hydration-card/hydration-card.component';
import { TdeeInsightCardComponent } from '../components/tdee-insight-card/tdee-insight-card.component';
import { WeightTrendCardComponent } from '../components/weight-trend-card/weight-trend-card.component';
import type {
    TdeeInsightDialogAction,
    TdeeInsightDialogComponent as TdeeInsightDialogComponentType,
    TdeeInsightDialogData,
} from '../dialogs/tdee-insight-dialog/tdee-insight-dialog.component';
import { DashboardFacade } from '../lib/dashboard.facade';
import {
    formatDashboardFastingDuration,
    getDashboardCyclicPhaseProgressLabel,
    getDashboardFastingCycleLabel,
    getDashboardFastingOccurrenceLabel,
    getDashboardFastingProtocolBaseLabel,
} from '../lib/dashboard-fasting.utils';
import { DashboardLayoutService } from '../lib/dashboard-layout.service';

@Component({
    selector: 'fd-dashboard',
    standalone: true,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiDatePickerButtonComponent,
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
    private readonly themeService = inject(ThemeService);
    private readonly facade = inject(DashboardFacade);
    private readonly translateService = inject(TranslateService);
    private readonly translate = (key: string, params?: Record<string, unknown>): string => this.translateService.instant(key, params);
    public readonly layout = inject(DashboardLayoutService);

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
        const stage = this.fastingBaseStage();
        const fallback = {
            progressPercent: this.facade.fastingProgressPercent(),
            elapsedFormatted: this.facade.fastingElapsedFormatted(),
            remainingFormatted: this.facade.fastingRemainingFormatted(),
            remainingLabelKey: 'FASTING.REMAINING',
            isOvertime: this.facade.fastingIsOvertime(),
            showStageProgress: true,
            stateLabel: getDashboardFastingOccurrenceLabel(this.translate, session?.occurrenceKind),
            detailLabel: session ? getDashboardFastingProtocolBaseLabel(this.translate, session) : null,
            metaLabel: session?.planType === 'Cyclic' ? getDashboardCyclicPhaseProgressLabel(this.translate, session, stage) : null,
            ringColor: stage?.color ?? null,
            stage,
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
        return visibleBlocks.some(block => ['hydration', 'cycle', 'weight', 'waist', 'tdee', 'advice'].includes(block));
    });

    public constructor() {
        this.facade.initialize();
        afterNextRender(() => {
            this.observeDashboardWidth();
        });
        const handler: UnsavedChangesHandler = {
            hasChanges: () => this.layout.hasLayoutChanges(),
            save: () => {
                this.layout.save();
            },
            discard: () => {
                this.layout.discard();
            },
        };
        this.unsavedChangesService.register(handler);
        this.destroyRef.onDestroy(() => {
            this.unsavedChangesService.clear(handler);
        });
    }

    public handleDateChange(value: Date): void {
        this.facade.setSelectedDate(value);
    }

    public async openWeightHistoryAsync(): Promise<void> {
        await this.navigationService.navigateToWeightHistoryAsync();
    }

    public async openWaistHistoryAsync(): Promise<void> {
        await this.navigationService.navigateToWaistHistoryAsync();
    }

    public async openCycleTrackingAsync(): Promise<void> {
        await this.navigationService.navigateToCycleTrackingAsync();
    }

    public async openGoalsAsync(): Promise<void> {
        await this.navigationService.navigateToGoalsAsync();
    }

    public async openProfileAsync(): Promise<void> {
        await this.navigationService.navigateToProfileAsync();
    }

    public async openCalorieGoalDialogAsync(): Promise<void> {
        const { CalorieGoalDialogComponent } = await import('../../goals/dialogs/calorie-goal-dialog/calorie-goal-dialog.component');
        this.dialogService
            .open(CalorieGoalDialogComponent, {
                preset: 'form',
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

    public async openNotificationSettingsAsync(): Promise<void> {
        const { DashboardNotificationSettingsDialogComponent } =
            await import('../dialogs/dashboard-notification-settings-dialog/dashboard-notification-settings-dialog.component');

        this.dialogService
            .open(DashboardNotificationSettingsDialogComponent, {
                preset: 'form',
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();
    }

    public async openAppearanceDialogAsync(): Promise<void> {
        const { DashboardAppearanceDialogComponent } =
            await import('../dialogs/dashboard-appearance-dialog/dashboard-appearance-dialog.component');

        this.dialogService
            .open(DashboardAppearanceDialogComponent, {
                size: 'md',
                data: {
                    theme: this.themeService.theme(),
                    uiStyle: this.themeService.uiStyle(),
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();
    }

    public async addConsumptionAsync(mealType?: string | null): Promise<void> {
        await this.navigationService.navigateToConsumptionAddAsync(mealType ?? undefined);
    }

    public async manageConsumptionsAsync(): Promise<void> {
        await this.navigationService.navigateToConsumptionListAsync();
    }

    public onMealCreated(): void {
        this.facade.reload(false);
    }

    public openConsumption(consumption: { id: string }): void {
        void this.navigationService.navigateToConsumptionEditAsync(consumption.id);
    }

    public addHydration(amount: number): void {
        this.facade.addHydration(amount);
    }

    public applyTdeeGoal(target: number): void {
        this.facade.applyTdeeGoal(target);
    }

    public handleTdeeCardClick(event?: Event): void {
        event?.stopPropagation();

        if (this.layout.isEditingLayout()) {
            this.layout.toggleBlock('tdee');
            return;
        }

        void this.openTdeeDetailsAsync();
    }

    public async openTdeeDetailsAsync(): Promise<void> {
        const { TdeeInsightDialogComponent } = await import('../dialogs/tdee-insight-dialog/tdee-insight-dialog.component');

        this.dialogService
            .open<TdeeInsightDialogComponentType, TdeeInsightDialogData, TdeeInsightDialogAction | undefined>(TdeeInsightDialogComponent, {
                size: 'md',
                data: {
                    insight: this.tdeeInsight(),
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(action => {
                switch (action?.type) {
                    case 'profile':
                        void this.openProfileAsync();
                        break;
                    case 'meal':
                        void this.addConsumptionAsync();
                        break;
                    case 'weight':
                        void this.openWeightHistoryAsync();
                        break;
                    case 'goals':
                        void this.openGoalsAsync();
                        break;
                    case 'applyGoal':
                        this.applyTdeeGoal(action.target);
                        break;
                }
            });
    }

    public handleFastingCardClick(): void {
        if (this.layout.isEditingLayout()) {
            this.layout.toggleBlock('fasting');
            return;
        }

        void this.openFastingAsync();
    }

    public async openFastingAsync(): Promise<void> {
        if (this.layout.isEditingLayout()) {
            return;
        }

        await this.navigationService.navigateToFastingAsync();
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
            const entry = entries[0] as ResizeObserverEntry | undefined;
            if (!entry) {
                return;
            }

            updateWidth(entry.contentRect.width);
        });

        this.resizeObserver.observe(element);
        this.destroyRef.onDestroy(() => this.resizeObserver?.disconnect());
    }
}
