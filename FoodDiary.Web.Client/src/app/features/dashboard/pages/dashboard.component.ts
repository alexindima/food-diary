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
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import type { AiInputBarResult } from '../../../components/shared/ai-input-bar/ai-input-bar.types';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../services/navigation.service';
import { ThemeService } from '../../../services/theme.service';
import { type UnsavedChangesHandler, UnsavedChangesService } from '../../../services/unsaved-changes.service';
import { AiMealCreateService } from '../../meals/lib/ai/ai-meal-create.service';
import type { TdeeInsightDialogComponent as TdeeInsightDialogComponentType } from '../dialogs/tdee-insight-dialog/tdee-insight-dialog.component';
import type {
    TdeeInsightDialogAction,
    TdeeInsightDialogData,
} from '../dialogs/tdee-insight-dialog/tdee-insight-dialog-lib/tdee-insight-dialog.types';
import { DashboardFacade } from '../lib/dashboard.facade';
import { DashboardLayoutService } from '../lib/dashboard-layout.service';
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
import { DashboardAdviceBlockComponent } from './dashboard-sections/dashboard-advice-block/dashboard-advice-block.component';
import { DashboardCycleBlockComponent } from './dashboard-sections/dashboard-cycle-block/dashboard-cycle-block.component';
import { DashboardEditHintComponent } from './dashboard-sections/dashboard-edit-hint/dashboard-edit-hint.component';
import { DashboardFastingBlockComponent } from './dashboard-sections/dashboard-fasting-block/dashboard-fasting-block.component';
import { DashboardHeaderComponent } from './dashboard-sections/dashboard-header/dashboard-header.component';
import { DashboardHydrationBlockComponent } from './dashboard-sections/dashboard-hydration-block/dashboard-hydration-block.component';
import { DashboardMealsBlockComponent } from './dashboard-sections/dashboard-meals-block/dashboard-meals-block.component';
import { DashboardQuickAddComponent } from './dashboard-sections/dashboard-quick-add/dashboard-quick-add.component';
import { DashboardSummaryBlockComponent } from './dashboard-sections/dashboard-summary-block/dashboard-summary-block.component';
import { DashboardTdeeBlockComponent } from './dashboard-sections/dashboard-tdee-block/dashboard-tdee-block.component';
import { DashboardTrendBlockComponent } from './dashboard-sections/dashboard-trend-block/dashboard-trend-block.component';

const FIRST_RESIZE_ENTRY_INDEX = 0;
const LANGUAGE_VERSION_INCREMENT = 1;

@Component({
    selector: 'fd-dashboard',
    host: {
        class: 'dashboard-host',
    },
    imports: [
        PageBodyComponent,
        FdPageContainerDirective,
        TranslatePipe,
        DashboardHeaderComponent,
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
    private readonly aiMealCreateService = inject(AiMealCreateService);
    private readonly facade = inject(DashboardFacade);
    public readonly layout = inject(DashboardLayoutService);
    private readonly languageVersion = signal(0);

    private readonly dashboardRoot = viewChild.required<ElementRef<HTMLElement>>('dashboardRoot');
    private resizeObserver: ResizeObserver | null = null;

    public readonly selectedDate = this.facade.selectedDate;
    public readonly isTodaySelected = this.facade.isTodaySelected;
    public readonly snapshot = this.facade.snapshot;
    public readonly isLoading = this.facade.isLoading;
    public readonly caloriesBurned = this.facade.caloriesBurned;
    public readonly meals = this.facade.meals;
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
        return visibleBlocks.some(block => isDashboardAsideBlock(block));
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

        return buildDashboardHeaderState(isToday, selectedDateLabel);
    });
    public readonly mealsPreviewState = computed<DashboardMealsPreviewState>(() => {
        this.languageVersion();
        const isToday = this.isTodaySelected();
        const selectedDateLabel = this.formatSelectedDate();
        const titleForDate = this.translateService.instant('DASHBOARD.MEALS_TITLE_FOR_DATE', { date: selectedDateLabel });

        return buildDashboardMealsPreviewState(isToday, titleForDate);
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
    public readonly dashboardSummaryData = computed<DashboardSummaryData>(() => {
        const ringData = this.consumptionRingData();

        return {
            dailyGoal: ringData.dailyGoal,
            dailyConsumed: ringData.dailyConsumed,
            weeklyConsumed: ringData.weeklyConsumed,
            weeklyGoal: ringData.weeklyGoal,
            nutrientBars: ringData.nutrientBars,
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

    public async openCycleTrackingAsync(): Promise<void> {
        await this.navigationService.navigateToCycleTrackingAsync();
    }

    public async openGoalsAsync(): Promise<void> {
        await this.navigationService.navigateToGoalsAsync();
    }

    public async openProfileAsync(): Promise<void> {
        await this.navigationService.navigateToProfileAsync();
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

    public onAiMealCreateRequested(result: AiInputBarResult): void {
        this.aiMealCreateService
            .createFromAiResult(result)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(meal => {
                if (meal !== null) {
                    this.onMealCreated();
                }
            });
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
