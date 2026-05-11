import { formatDate } from '@angular/common';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    type ElementRef,
    inject,
    signal,
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
import { NavigationService } from '../../../services/navigation.service';
import { ThemeService } from '../../../services/theme.service';
import { type UnsavedChangesHandler, UnsavedChangesService } from '../../../services/unsaved-changes.service';
import { FastingTimerCardComponent } from '../../fasting/components/fasting-timer-card/fasting-timer-card.component';
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
import { DashboardLayoutService } from '../lib/dashboard-layout.service';

const EMPTY_INDEX = -1;
const FIRST_RESIZE_ENTRY_INDEX = 0;
const LANGUAGE_VERSION_INCREMENT = 1;

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
    private readonly translateService = inject(TranslateService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly themeService = inject(ThemeService);
    private readonly facade = inject(DashboardFacade);
    public readonly layout = inject(DashboardLayoutService);
    private readonly languageVersion = signal(0);

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
        return visibleBlocks.some(block => this.isAsideBlock(block));
    });
    public readonly editSaveActionLabelKey = computed(() => (this.layout.hasLayoutChanges() ? 'DASHBOARD.SETTINGS.SAVE' : null));
    public readonly editSaveActionLabel = computed(() => {
        const labelKey = this.editSaveActionLabelKey();
        return labelKey !== null ? this.translateService.instant(labelKey) : null;
    });
    public readonly dashboardHeaderState = computed<DashboardHeaderState>(() => {
        this.languageVersion();
        const isToday = this.isTodaySelected();
        const selectedDateLabel = this.formatSelectedDate();

        return {
            isToday,
            fullTitleKey: isToday ? 'DASHBOARD.TITLE' : 'DASHBOARD.TITLE_FOR_DATE',
            compactTitleKey: isToday ? 'DASHBOARD.TITLE_SHORT' : 'DASHBOARD.TITLE_FOR_DATE_SHORT',
            titleParams: isToday ? null : { date: selectedDateLabel },
            selectedDateLabel,
        };
    });
    public readonly mealsPreviewState = computed(() => {
        this.languageVersion();
        const isToday = this.isTodaySelected();
        const selectedDateLabel = this.formatSelectedDate();

        return {
            titleText: isToday ? null : this.translateService.instant('DASHBOARD.MEALS_TITLE_FOR_DATE', { date: selectedDateLabel }),
            emptyKey: isToday ? 'DASHBOARD.MEALS_EMPTY' : 'DASHBOARD.MEALS_EMPTY_FOR_DATE',
            showDateActions: isToday,
            showEmptyState: !isToday,
        };
    });
    public readonly hydrationCardState = computed(() => {
        const hydration = this.hydration();

        return {
            total: hydration?.totalMl ?? 0,
            goal: hydration?.goalMl ?? null,
        };
    });
    public readonly cycleCardState = computed(() => {
        const cycle = this.cycle();

        return {
            startDate: cycle?.startDate ?? null,
            predictions: cycle?.predictions ?? null,
        };
    });
    public readonly dashboardBlockStates = computed<Record<DashboardBlockId, DashboardBlockState>>(() => {
        this.languageVersion();
        const editing = this.layout.isEditingLayout();

        const stateFor = (blockId: DashboardBlockId, options: DashboardBlockStateOptions = {}): DashboardBlockState =>
            this.buildDashboardBlockState(blockId, editing, options);

        return {
            fasting: stateFor('fasting', {
                alwaysInteractive: true,
                editingLabelKey: 'DASHBOARD.FASTING_EDIT_TITLE',
                defaultLabelKey: 'FASTING.TITLE',
            }),
            summary: stateFor('summary', { locked: true }),
            meals: stateFor('meals'),
            hydration: stateFor('hydration'),
            cycle: stateFor('cycle'),
            weight: stateFor('weight'),
            waist: stateFor('waist'),
            tdee: stateFor('tdee', { alwaysInteractive: true, defaultLabelKey: 'TDEE_CARD.TITLE' }),
            advice: stateFor('advice'),
        };
    });

    public constructor() {
        this.facade.initialize();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + LANGUAGE_VERSION_INCREMENT);
        });
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

    public handleDateChange(value: Date | null): void {
        if (value === null) {
            return;
        }

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
                    dailyCalorieTarget: this.dailyGoal() > 0 ? this.dailyGoal() : null,
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(saved => {
                if (saved === true) {
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
                    case undefined:
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
            const entry = entries[FIRST_RESIZE_ENTRY_INDEX] as ResizeObserverEntry | undefined;
            if (entry === undefined) {
                return;
            }

            updateWidth(entry.contentRect.width);
        });

        this.resizeObserver.observe(element);
        this.destroyRef.onDestroy(() => this.resizeObserver?.disconnect());
    }

    private formatSelectedDate(): string {
        return formatDate(this.selectedDate(), 'd MMMM y', this.translateService.getCurrentLang());
    }

    private buildDashboardBlockState(
        blockId: DashboardBlockId,
        editing: boolean,
        options: DashboardBlockStateOptions,
    ): DashboardBlockState {
        const isVisible = this.layout.isBlockVisible(blockId);
        const isAlwaysInteractive = options.alwaysInteractive === true;
        const ariaLabelKey = editing ? (options.editingLabelKey ?? null) : (options.defaultLabelKey ?? null);
        return {
            hidden: editing && !isVisible,
            role: editing || isAlwaysInteractive ? 'button' : null,
            tabIndex: editing || isAlwaysInteractive ? 0 : EMPTY_INDEX,
            ariaPressed: editing ? isVisible : null,
            ariaDisabled: editing && options.locked === true ? !this.layout.canToggleBlock(blockId) : null,
            ariaLabel: ariaLabelKey !== null ? this.translateService.instant(ariaLabelKey) : null,
            inert: editing ? '' : null,
        };
    }

    private isAsideBlock(blockId: string): boolean {
        return (
            blockId === 'hydration' ||
            blockId === 'cycle' ||
            blockId === 'weight' ||
            blockId === 'waist' ||
            blockId === 'tdee' ||
            blockId === 'advice'
        );
    }
}

interface DashboardHeaderState {
    isToday: boolean;
    fullTitleKey: string;
    compactTitleKey: string;
    titleParams: { date: string } | null;
    selectedDateLabel: string;
}

type DashboardBlockId = 'fasting' | 'summary' | 'meals' | 'hydration' | 'cycle' | 'weight' | 'waist' | 'tdee' | 'advice';

interface DashboardBlockState {
    hidden: boolean;
    role: 'button' | null;
    tabIndex: number;
    ariaPressed: boolean | null;
    ariaDisabled: boolean | null;
    ariaLabel: string | null;
    inert: string | null;
}

interface DashboardBlockStateOptions {
    alwaysInteractive?: boolean;
    locked?: boolean;
    editingLabelKey?: string;
    defaultLabelKey?: string;
}
