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
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDatePickerButtonComponent } from 'fd-ui-kit/date-picker-button/fd-ui-date-picker-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

import type { AiInputBarResult } from '../../../components/shared/ai-input-bar/ai-input-bar.types';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header';
import { NavigationService } from '../../../services/navigation.service';
import { type UnsavedChangesHandler, UnsavedChangesService } from '../../../services/unsaved-changes.service';
import { ViewportService } from '../../../shared/platform/viewport.service';
import { ThemeService } from '../../../shared/theme/theme.service';
import { FdPageContainerDirective } from '../../../shared/ui/layout/page-container.directive';
import { AiMealCreateFacade } from '../../meals/lib/ai/ai-meal-create.facade';
import type { TdeeInsightDialogComponent as TdeeInsightDialogComponentType } from '../dialogs/tdee-insight-dialog/tdee-insight-dialog';
import type {
    TdeeInsightDialogAction,
    TdeeInsightDialogData,
} from '../dialogs/tdee-insight-dialog/tdee-insight-dialog-lib/tdee-insight-dialog.types';
import { DashboardFacade } from '../lib/dashboard.facade';
import { DashboardLayoutService } from '../lib/dashboard-layout.service';
import { DASHBOARD_FIRST_RESIZE_ENTRY_INDEX, DASHBOARD_LANGUAGE_VERSION_INCREMENT } from './dashboard-lib/dashboard-page.config';
import type {
    DashboardBlockId,
    DashboardBlockState,
    DashboardBlockStateOptions,
    DashboardHeaderState,
    DashboardMealsPreviewState,
    DashboardSummaryData,
} from './dashboard-lib/dashboard-view.types';
import {
    buildDashboardBlockState,
    buildDashboardHeaderState,
    buildDashboardMealsPreviewState,
    isDashboardAsideBlock,
} from './dashboard-lib/dashboard-view-state.mapper';
import { DashboardAdviceBlockComponent } from './dashboard-sections/dashboard-advice-block/dashboard-advice-block';
import { DashboardCycleBlockComponent } from './dashboard-sections/dashboard-cycle-block/dashboard-cycle-block';
import { DashboardEditHintComponent } from './dashboard-sections/dashboard-edit-hint/dashboard-edit-hint';
import { DashboardFastingBlockComponent } from './dashboard-sections/dashboard-fasting-block/dashboard-fasting-block';
import { DashboardHydrationBlockComponent } from './dashboard-sections/dashboard-hydration-block/dashboard-hydration-block';
import { DashboardMealsBlockComponent } from './dashboard-sections/dashboard-meals-block/dashboard-meals-block';
import { DashboardQuickAddComponent } from './dashboard-sections/dashboard-quick-add/dashboard-quick-add';
import { DashboardSummaryBlockComponent } from './dashboard-sections/dashboard-summary-block/dashboard-summary-block';
import { DashboardTdeeBlockComponent } from './dashboard-sections/dashboard-tdee-block/dashboard-tdee-block';
import { DashboardTrendBlockComponent } from './dashboard-sections/dashboard-trend-block/dashboard-trend-block';

@Component({
    selector: 'fd-dashboard',
    host: {
        class: 'dashboard-host',
    },
    imports: [
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiDatePickerButtonComponent,
        DashboardQuickAddComponent,
        DashboardEditHintComponent,
        DashboardFastingBlockComponent,
        DashboardSummaryBlockComponent,
        DashboardMealsBlockComponent,
        DashboardHydrationBlockComponent,
        DashboardCycleBlockComponent,
        DashboardTrendBlockComponent,
        DashboardTdeeBlockComponent,
        DashboardAdviceBlockComponent,
    ],
    providers: [DashboardLayoutService, DashboardFacade],
    templateUrl: './dashboard.html',
    styleUrl: './dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly themeService = inject(ThemeService);
    private readonly viewportService = inject(ViewportService);
    private readonly aiMealCreateFacade = inject(AiMealCreateFacade);
    private readonly facade = inject(DashboardFacade);
    protected readonly layout = inject(DashboardLayoutService);
    private readonly languageVersion = signal(0);

    private readonly dashboardRoot = viewChild.required<ElementRef<HTMLElement>>('dashboardRoot');
    private resizeObserver: ResizeObserver | null = null;

    protected readonly selectedDate = this.facade.selectedDate;
    protected readonly isTodaySelected = this.facade.isTodaySelected;
    protected readonly snapshot = this.facade.snapshot;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly caloriesBurned = this.facade.caloriesBurned;
    protected readonly meals = this.facade.meals;
    protected readonly weeklyConsumed = this.facade.weeklyConsumed;
    protected readonly hydration = this.facade.hydration;
    protected readonly dailyAdvice = this.facade.dailyAdvice;
    protected readonly isHydrationLoading = this.facade.isHydrationLoading;
    protected readonly isWeightTrendLoading = this.facade.isWeightTrendLoading;
    protected readonly isWaistTrendLoading = this.facade.isWaistTrendLoading;
    protected readonly isAdviceLoading = this.facade.isAdviceLoading;
    protected readonly cycle = this.facade.cycle;
    protected readonly isCycleLoading = this.facade.isCycleLoading;
    protected readonly tdeeInsight = this.facade.tdeeInsight;
    protected readonly weightTrend = this.facade.weightTrend;
    protected readonly waistTrend = this.facade.waistTrend;
    protected readonly nutrientBars = this.facade.nutrientBars;
    protected readonly consumptionRingData = this.facade.consumptionRingData;
    protected readonly mealPreviewEntries = this.facade.mealPreviewEntries;
    protected readonly placeholderIcon = this.facade.placeholderIcon;
    protected readonly placeholderLabel = this.facade.placeholderLabel;
    protected readonly fastingIsActive = this.facade.fastingIsActive;
    protected readonly fastingCurrentSession = this.facade.currentFastingSession;
    protected readonly isAiMealSaving = this.aiMealCreateFacade.isSaving;
    protected readonly aiMealClearToken = this.aiMealCreateFacade.clearToken;
    protected readonly shouldRenderFastingWidget = computed(() => {
        if (this.layout.isEditingLayout()) {
            return this.layout.shouldRenderBlock('fasting');
        }

        return this.isTodaySelected() && this.layout.isBlockVisible('fasting') && this.fastingIsActive();
    });
    protected readonly hasRenderedAsideBlocks = computed(() => {
        if (this.layout.isEditingLayout()) {
            return this.layout.hasAsideBlocks();
        }

        const visibleBlocks = this.layout.visibleBlocks();
        return visibleBlocks.some(block => isDashboardAsideBlock(block));
    });
    protected readonly editSaveActionLabelKey = computed(() => (this.layout.hasLayoutChanges() ? 'DASHBOARD.SETTINGS.SAVE' : null));
    protected readonly editSaveActionLabel = computed(() => {
        const labelKey = this.editSaveActionLabelKey();
        return labelKey !== null ? this.translateService.instant(labelKey) : null;
    });
    protected readonly dashboardHeaderState = computed<DashboardHeaderState>(() => {
        this.languageVersion();
        const isToday = this.isTodaySelected();
        const selectedDateLabel = this.formatSelectedDate();

        return buildDashboardHeaderState(isToday, selectedDateLabel);
    });
    protected readonly dashboardTitle = computed(() => {
        const headerState = this.dashboardHeaderState();
        const titleKey = this.viewportService.isMobile() ? headerState.compactTitleKey : headerState.fullTitleKey;

        return this.translateService.instant(titleKey, headerState.titleParams ?? undefined);
    });
    protected readonly mealsPreviewState = computed<DashboardMealsPreviewState>(() => {
        this.languageVersion();
        const isToday = this.isTodaySelected();
        const selectedDateLabel = this.formatSelectedDate();
        const titleForDate = this.translateService.instant('DASHBOARD.MEALS_TITLE_FOR_DATE', { date: selectedDateLabel });

        return buildDashboardMealsPreviewState(isToday, titleForDate);
    });
    protected readonly hydrationCardState = computed(() => {
        const hydration = this.hydration();

        return {
            total: hydration?.totalMl ?? 0,
            goal: hydration?.goalMl ?? null,
        };
    });
    protected readonly cycleCardState = computed(() => {
        const cycle = this.cycle();

        return {
            startDate: cycle?.trackingStartDate ?? null,
            predictions: cycle?.predictions ?? null,
        };
    });
    protected readonly dashboardSummaryData = computed<DashboardSummaryData>(() => {
        const ringData = this.consumptionRingData();

        return {
            dailyGoal: ringData.dailyGoal,
            dailyConsumed: ringData.dailyConsumed,
            weeklyConsumed: ringData.weeklyConsumed,
            weeklyGoal: ringData.weeklyGoal,
            nutrientBars: ringData.nutrientBars,
        };
    });
    protected readonly dashboardBlockStates = computed<Record<DashboardBlockId, DashboardBlockState>>(() => {
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
            this.languageVersion.update(version => version + DASHBOARD_LANGUAGE_VERSION_INCREMENT);
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

    protected changeDashboardDate(value: Date | null): void {
        if (value === null) {
            return;
        }

        this.facade.setSelectedDate(value);
    }

    protected async openWeightHistoryAsync(): Promise<void> {
        await this.navigationService.navigateToWeightHistoryAsync();
    }

    protected async openCycleTrackingAsync(): Promise<void> {
        await this.navigationService.navigateToCycleTrackingAsync();
    }

    protected async openGoalsAsync(): Promise<void> {
        await this.navigationService.navigateToGoalsAsync();
    }

    protected async openProfileAsync(): Promise<void> {
        await this.navigationService.navigateToProfileAsync();
    }

    protected async openNotificationSettingsAsync(): Promise<void> {
        const { DashboardNotificationSettingsDialogComponent } =
            await import('../dialogs/dashboard-notification-settings-dialog/dashboard-notification-settings-dialog');

        this.dialogService
            .open(DashboardNotificationSettingsDialogComponent, {
                preset: 'form',
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();
    }

    protected async openAppearanceDialogAsync(): Promise<void> {
        const { DashboardAppearanceDialogComponent } = await import('../dialogs/dashboard-appearance-dialog/dashboard-appearance-dialog');

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

    protected async addConsumptionAsync(mealType?: string | null): Promise<void> {
        await this.navigationService.navigateToConsumptionAddAsync(mealType ?? undefined);
    }

    protected async manageConsumptionsAsync(): Promise<void> {
        await this.navigationService.navigateToConsumptionListAsync();
    }

    protected onMealCreated(): void {
        this.facade.reload(false);
    }

    protected onAiMealCreateRequested(result: AiInputBarResult): void {
        this.aiMealCreateFacade
            .createFromAiResult(result)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(meal => {
                if (meal !== null) {
                    this.onMealCreated();
                }
            });
    }

    protected openConsumption(consumption: { id: string }): void {
        void this.navigationService.navigateToConsumptionEditAsync(consumption.id);
    }

    protected addHydration(amount: number): void {
        this.facade.addHydration(amount);
    }

    protected applyTdeeGoal(target: number): void {
        this.facade.applyTdeeGoal(target);
    }

    protected openTdeeInsight(event?: Event): void {
        event?.stopPropagation();

        if (this.layout.isEditingLayout()) {
            this.layout.toggleBlock('tdee');
            return;
        }

        void this.openTdeeDetailsAsync();
    }

    protected async openTdeeDetailsAsync(): Promise<void> {
        const { TdeeInsightDialogComponent } = await import('../dialogs/tdee-insight-dialog/tdee-insight-dialog');

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
                    case 'profile': {
                        void this.openProfileAsync();
                        break;
                    }
                    case 'meal': {
                        void this.addConsumptionAsync();
                        break;
                    }
                    case 'weight': {
                        void this.openWeightHistoryAsync();
                        break;
                    }
                    case 'goals': {
                        void this.openGoalsAsync();
                        break;
                    }
                    case 'applyGoal': {
                        this.applyTdeeGoal(action.target);
                        break;
                    }
                    case undefined: {
                        break;
                    }
                }
            });
    }

    protected openFastingPage(): void {
        if (this.layout.isEditingLayout()) {
            this.layout.toggleBlock('fasting');
            return;
        }

        void this.openFastingAsync();
    }

    protected async openFastingAsync(): Promise<void> {
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
            const entry = entries[DASHBOARD_FIRST_RESIZE_ENTRY_INDEX] as ResizeObserverEntry | undefined;
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
        return buildDashboardBlockState({
            blockId,
            editing,
            isVisible: this.layout.isBlockVisible(blockId),
            canToggle: this.layout.canToggleBlock(blockId),
            ariaLabel: this.resolveDashboardBlockAriaLabel(editing, options),
            stateOptions: options,
        });
    }

    private resolveDashboardBlockAriaLabel(editing: boolean, options: DashboardBlockStateOptions): string | null {
        const ariaLabelKey = editing ? (options.editingLabelKey ?? null) : (options.defaultLabelKey ?? null);
        return ariaLabelKey !== null ? this.translateService.instant(ariaLabelKey) : null;
    }
}
