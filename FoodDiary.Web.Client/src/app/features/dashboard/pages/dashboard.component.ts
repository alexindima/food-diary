import {
    AfterViewInit,
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    ElementRef,
    OnInit,
    computed,
    inject,
    viewChild,
} from '@angular/core';
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
import { FASTING_PROTOCOLS, FastingOccurrenceKind, FastingSession } from '../../fasting/models/fasting.data';
import { AiInputBarComponent } from '../../../components/shared/ai-input-bar/ai-input-bar.component';

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
    ],
    providers: [DashboardLayoutService, DashboardFacade, provideCharts(withDefaultRegisterables())],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit, AfterViewInit {
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly facade = inject(DashboardFacade);
    private readonly translateService = inject(TranslateService);
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
            stateLabel: this.getOccurrenceKindLabel(session?.occurrenceKind),
            detailLabel: session ? this.getFastingProtocolBaseLabel(session) : null,
            metaLabel: session?.planType === 'Cyclic' ? this.getCyclicPhaseProgressLabel(session) : null,
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
                elapsedFormatted: this.formatDuration(cycleElapsedMs),
                remainingFormatted: this.formatDuration(Math.max(0, fastWindowMs - cycleElapsedMs)),
                remainingLabelKey: 'FASTING.UNTIL_EATING_WINDOW',
                isOvertime: false,
                showStageProgress: true,
                stateLabel: this.translateService.instant('FASTING.FASTING_WINDOW'),
                detailLabel: this.getFastingProtocolBaseLabel(session),
                metaLabel: this.getFastingCycleLabel(cycleDay),
                ringColor: stage.color,
                stage,
            };
        }

        const eatingElapsedMs = cycleElapsedMs - fastWindowMs;
        return {
            progressPercent: Math.min((eatingElapsedMs / eatingWindowMs) * 100, 100),
            elapsedFormatted: this.formatDuration(eatingElapsedMs),
            remainingFormatted: this.formatDuration(Math.max(0, eatingWindowMs - eatingElapsedMs)),
            remainingLabelKey: 'FASTING.NEXT_FAST',
            isOvertime: false,
            showStageProgress: false,
            stateLabel: this.translateService.instant('FASTING.EATING_WINDOW'),
            detailLabel: this.getFastingProtocolBaseLabel(session),
            metaLabel: this.getFastingCycleLabel(cycleDay),
            ringColor: '#22c55e',
            stage: {
                index: cycleDay,
                total: cycleDay,
                titleKey: 'FASTING.EATING_WINDOW',
                descriptionKey: 'FASTING.EATING_WINDOW_DESCRIPTION',
                color: '#22c55e',
                glowColor: 'rgba(34, 197, 94, 0.18)',
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

        return this.formatDuration(stage.nextInMs);
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

    public ngAfterViewInit(): void {
        this.observeDashboardWidth();
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

    private getOccurrenceKindLabel(kind?: FastingOccurrenceKind | null): string | null {
        switch (kind) {
            case 'FastDay':
                return this.translateService.instant('FASTING.FAST_DAY');
            case 'EatDay':
                return this.translateService.instant('FASTING.EAT_DAY');
            case 'FastingWindow':
                return this.translateService.instant('FASTING.FASTING_WINDOW');
            case 'EatingWindow':
                return this.translateService.instant('FASTING.EATING_WINDOW');
            default:
                return null;
        }
    }

    private getFastingProtocolDisplay(session: FastingSession, cycleDay: number | null): string {
        if (session.planType === 'Cyclic') {
            const cycleLabel =
                session.cyclicFastDays && session.cyclicEatDays ? `${session.cyclicFastDays}:${session.cyclicEatDays}` : '1:1';
            const eatWindowHours = session.cyclicEatDayEatingWindowHours ?? 8;
            const eatFastHours = session.cyclicEatDayFastHours ?? 16;
            return `${cycleLabel} (${eatFastHours}:${eatWindowHours})`;
        }

        const option = FASTING_PROTOCOLS.find(item => item.value === session.protocol);
        const hoursLabel = this.translateService.instant('FASTING.HOURS');

        if (!option) {
            return this.formatHoursWithExtension(session.initialPlannedDurationHours, session.addedDurationHours, hoursLabel);
        }

        if (option.value === 'CustomIntermittent') {
            const ratioLabel = this.getIntermittentRatioLabel(session.initialPlannedDurationHours);
            return cycleDay ? `${ratioLabel} · ${this.translateService.instant('FASTING.DAY_LABEL', { day: cycleDay })}` : ratioLabel;
        }

        const baseLabel =
            option.value === 'Custom'
                ? `${session.initialPlannedDurationHours} ${hoursLabel}`
                : this.translateService.instant(option.labelKey);

        const resolvedLabel = session.addedDurationHours > 0 ? `${baseLabel} (+${session.addedDurationHours} ${hoursLabel})` : baseLabel;
        if (session.planType === 'Intermittent' && cycleDay) {
            return `${resolvedLabel} · ${this.translateService.instant('FASTING.DAY_LABEL', { day: cycleDay })}`;
        }

        return resolvedLabel;
    }

    private formatHoursWithExtension(baseHours: number, addedHours: number, hoursLabel: string): string {
        return addedHours > 0 ? `${baseHours} ${hoursLabel} (+${addedHours} ${hoursLabel})` : `${baseHours} ${hoursLabel}`;
    }

    private getIntermittentRatioLabel(fastHours: number): string {
        const normalizedFastHours = Math.max(1, Math.min(23, fastHours));
        const eatingWindowHours = Math.max(1, 24 - normalizedFastHours);
        return `${normalizedFastHours}:${eatingWindowHours}`;
    }

    private getFastingProtocolBaseLabel(session: FastingSession): string {
        if (session.planType === 'Cyclic') {
            const cycleLabel =
                session.cyclicFastDays && session.cyclicEatDays ? `${session.cyclicFastDays}:${session.cyclicEatDays}` : '1:1';
            const eatWindowHours = session.cyclicEatDayEatingWindowHours ?? 8;
            const eatFastHours = session.cyclicEatDayFastHours ?? 16;
            return `${cycleLabel} (${eatFastHours}:${eatWindowHours})`;
        }

        const option = FASTING_PROTOCOLS.find(item => item.value === session.protocol);
        const hoursLabel = this.translateService.instant('FASTING.HOURS');

        if (!option) {
            return this.formatHoursWithExtension(session.initialPlannedDurationHours, session.addedDurationHours, hoursLabel);
        }

        if (option.value === 'CustomIntermittent') {
            return this.getIntermittentRatioLabel(session.initialPlannedDurationHours);
        }

        const baseLabel =
            option.value === 'Custom'
                ? `${session.initialPlannedDurationHours} ${hoursLabel}`
                : this.translateService.instant(option.labelKey);

        return session.addedDurationHours > 0 ? `${baseLabel} (+${session.addedDurationHours} ${hoursLabel})` : baseLabel;
    }

    private getFastingCycleLabel(cycleDay: number | null): string | null {
        return cycleDay ? this.translateService.instant('FASTING.DAY_LABEL', { day: cycleDay }) : null;
    }

    private getCyclicPhaseProgressLabel(session: FastingSession): string | null {
        const dayNumber = session.cyclicPhaseDayNumber;
        const dayTotal = session.cyclicPhaseDayTotal;
        if (!dayNumber || !dayTotal) {
            return this.getOccurrenceKindLabel(session.occurrenceKind);
        }

        const key = session.occurrenceKind === 'EatDay' ? 'FASTING.CYCLIC_EAT_PHASE_PROGRESS' : 'FASTING.CYCLIC_FAST_PHASE_PROGRESS';

        return this.translateService.instant(key, { current: dayNumber, total: dayTotal });
    }

    private formatDuration(ms: number): string {
        const totalSeconds = Math.floor(ms / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
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
