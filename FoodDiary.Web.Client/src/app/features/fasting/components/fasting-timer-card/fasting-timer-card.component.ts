import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame.component';
import { LocalizationService } from '../../../../services/localization.service';
import { FastingFacade } from '../../lib/fasting.facade';
import { buildFastingTimerCardComputedState } from '../../lib/fasting-timer-card-state';
import type { FastingOccurrenceKind, FastingSession } from '../../models/fasting.data';
import { FastingControlsComponent } from '../fasting-controls/fasting-controls.component';
import { type FastingTimerCardDisplayGroup, FastingTimerCardGroupsComponent } from './fasting-timer-card-groups.component';
import { type FastingTimerCardDisplayItem, FastingTimerCardItemsComponent } from './fasting-timer-card-items.component';

interface FastingTimerCardState {
    isActive: boolean;
    isOvertime: boolean;
    currentSessionCompleted: boolean;
    progressPercent: number;
    elapsedFormatted: string;
    remainingFormatted: string;
    remainingLabelKey: string;
    labelKey: string;
    stateLabel: string | null;
    occurrenceKind: FastingOccurrenceKind | null;
    detailLabel: string | null;
    metaLabel: string | null;
    ringColor: string | null;
    glowColor: string | null;
    stageTitleKey: string | null;
    stageDescriptionKey: string | null;
    stageIndex: number | null;
    totalStages: number;
    nextStageTitleKey: string | null;
    nextStageFormatted: string | null;
    showGlow: boolean;
}

const RING_RADIUS = 90;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;
const PERCENT_FULL = 100;
const TIMER_TICK_MS = 1000;
const EMPTY_DURATION_MS = 0;

@Component({
    selector: 'fd-fasting-timer-card',
    standalone: true,
    imports: [
        NgTemplateOutlet,
        TranslatePipe,
        FdUiCardComponent,
        DashboardWidgetFrameComponent,
        FastingControlsComponent,
        FastingTimerCardItemsComponent,
        FastingTimerCardGroupsComponent,
    ],
    templateUrl: './fasting-timer-card.component.html',
    styleUrl: './fasting-timer-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingTimerCardComponent {
    private readonly facade = inject(FastingFacade, { optional: true });
    private readonly destroyRef = inject(DestroyRef);
    private readonly localizationService = inject(LocalizationService);
    private readonly translateService = inject(TranslateService);
    private readonly translate = (key: string, params?: Record<string, unknown>): string => this.translateService.instant(key, params);
    private readonly currentLanguage = signal(this.localizationService.getCurrentLanguage());
    private readonly now = signal(new Date());
    private timerInterval: ReturnType<typeof setInterval> | null = null;
    private readonly timerEffect = effect(() => {
        const session = this.getSession();
        if (!this.usesFacadeTimer() && session !== null && session.endedAtUtc === null) {
            this.startTimer();
            return;
        }

        this.stopTimer();
    });
    protected readonly normalizedProgressPercent = computed(() => {
        const progress = this.viewState().progressPercent;
        return Number.isFinite(progress) ? Math.max(EMPTY_DURATION_MS, Math.min(progress, PERCENT_FULL)) : EMPTY_DURATION_MS;
    });
    protected readonly ringStrokeDasharray = RING_CIRCUMFERENCE;
    protected readonly ringStrokeDashoffset = computed(() => RING_CIRCUMFERENCE * (1 - this.normalizedProgressPercent() / PERCENT_FULL));
    public readonly layout = input<'dashboard' | 'page'>('page');
    public readonly session = input<FastingSession | null>(null);
    private readonly usesFacadeTimer = computed(() => this.layout() === 'page' && this.facade !== null);
    protected readonly viewState = computed<FastingTimerCardState>(() => {
        this.currentLanguage();
        return this.buildViewState();
    });

    protected readonly isDashboardLayout = computed(() => this.layout() === 'dashboard');
    protected readonly isSetupLayout = computed(() => this.layout() === 'page' && !this.viewState().isActive);
    protected readonly isPageSummaryLayout = computed(() => this.layout() === 'page' && this.viewState().isActive);
    protected readonly cardChrome = computed<FastingTimerCardChrome>(() => ({
        density: this.isDashboardLayout() || this.isPageSummaryLayout() ? 'compact' : 'default',
        title: this.layout() === 'page' ? this.translateService.instant('FASTING.TITLE') : '',
        showPageControls: this.layout() === 'page',
    }));
    protected readonly isEatingPhase = computed(() => {
        const { occurrenceKind } = this.viewState();
        return occurrenceKind === 'EatDay' || occurrenceKind === 'EatingWindow';
    });
    protected readonly shouldShowStageProgress = computed(() => {
        const state = this.viewState();
        return !this.isEatingPhase() && state.stageTitleKey !== null && state.stageIndex !== null && state.totalStages > 0;
    });
    protected readonly shouldShowStageDescriptionFallback = computed(
        () => !this.isEatingPhase() && !this.shouldShowStageProgress() && this.viewState().stageDescriptionKey !== null,
    );
    protected readonly mainRingItems = computed(() => this.buildMainRingItems());
    protected readonly summaryRingItems = computed(() => this.buildSummaryRingItems());
    protected readonly summaryContentGroups = computed(() => this.buildSummaryContentGroups());
    protected readonly progressStrokeColor = computed(() => {
        const state = this.viewState();
        if (state.isOvertime || this.isEatingPhase()) {
            return 'var(--fd-color-green-500)';
        }

        return state.ringColor ?? 'var(--fd-color-purple-500)';
    });
    protected readonly ringGlow = computed(() => {
        const state = this.viewState();
        if (!state.showGlow) {
            return null;
        }

        if (state.isOvertime) {
            return 'var(--fd-shadow-fasting-overtime-ring)';
        }

        const glowColor = state.glowColor;
        if (glowColor === null || glowColor.length === 0) {
            return null;
        }

        return `0 0 0 var(--fd-size-fasting-ring-glow-spread) ${glowColor}, 0 var(--fd-size-fasting-ring-glow-offset-y) var(--fd-size-fasting-ring-glow-blur) ${glowColor}`;
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.currentLanguage.set(this.localizationService.getCurrentLanguage());
        });

        this.destroyRef.onDestroy(() => {
            this.stopTimer();
        });
    }

    private buildViewState(): FastingTimerCardState {
        const session = this.getSession();
        const isActive = session !== null && session.endedAtUtc === null;
        const state = buildFastingTimerCardComputedState({
            session,
            elapsedMs: this.getElapsedMs(),
            translate: this.translate,
        });

        return {
            isActive,
            isOvertime: state.isOvertime,
            currentSessionCompleted: session?.endedAtUtc !== null && session?.endedAtUtc !== undefined,
            progressPercent: state.progressPercent,
            elapsedFormatted: state.elapsedFormatted,
            remainingFormatted: state.remainingFormatted,
            remainingLabelKey: state.remainingLabelKey,
            labelKey: this.getCardLabelKey(session),
            stateLabel: state.stateLabel,
            occurrenceKind: session?.occurrenceKind ?? null,
            detailLabel: state.detailLabel,
            metaLabel: state.metaLabel,
            ringColor: state.ringColor,
            ...this.buildStageViewState(state),
            showGlow: this.layout() === 'page' && !isActive,
        };
    }

    private buildMainRingItems(): FastingTimerCardDisplayItem[] {
        const state = this.viewState();
        const items: FastingTimerCardDisplayItem[] = [];
        const showHeader = !(this.isSetupLayout() && !state.isActive && !state.currentSessionCompleted);

        if (showHeader) {
            items.push({ className: 'fasting-timer-card__label fd-ui-meta-text', text: this.translate(state.labelKey) });
            this.pushOptionalItem(items, 'fasting-timer-card__state fd-ui-meta-text', state.stateLabel);
        }

        if (state.isActive) {
            this.addActiveMainRingItems(items, state);
        } else if (state.currentSessionCompleted) {
            this.addCompletedMainRingItems(items, state);
        } else if (this.isSetupLayout()) {
            items.push({ className: 'fasting-timer-card__remaining fd-ui-body-sm', text: this.translate('FASTING.SELECT_AND_START') });
        } else {
            items.push({
                className: 'fasting-timer-card__elapsed fasting-timer-card__elapsed--idle fd-ui-metric-lg',
                text: this.translate('FASTING.READY'),
            });
            items.push({ className: 'fasting-timer-card__remaining fd-ui-body-sm', text: this.translate('FASTING.SELECT_AND_START') });
        }

        if (showHeader) {
            this.pushOptionalItem(items, 'fasting-timer-card__detail fd-ui-caption', state.detailLabel);
        }

        return items;
    }

    private addActiveMainRingItems(items: FastingTimerCardDisplayItem[], state: FastingTimerCardState): void {
        items.push({ className: 'fasting-timer-card__elapsed fd-ui-metric-lg', text: state.elapsedFormatted });
        this.addStageItems(items, state);
        items.push({ className: 'fasting-timer-card__remaining fd-ui-body-sm', text: this.getRemainingText(state) });

        if (!this.isEatingPhase() && !state.isOvertime && state.nextStageTitleKey !== null && state.nextStageFormatted !== null) {
            items.push({
                className: 'fasting-timer-card__next-stage-time fd-ui-body-sm',
                text: this.translate('FASTING.STAGES.NEXT_IN', { time: state.nextStageFormatted }),
            });
            items.push({
                className: 'fasting-timer-card__next-stage-label',
                text: `${this.translate('FASTING.STAGES.NEXT_STAGE')}: ${this.translate(state.nextStageTitleKey)}`,
            });
            return;
        }

        this.addStageDescriptionFallbackItem(items, state);
    }

    private addCompletedMainRingItems(items: FastingTimerCardDisplayItem[], state: FastingTimerCardState): void {
        items.push({ className: 'fasting-timer-card__elapsed fd-ui-metric-lg', text: state.elapsedFormatted });
        this.addStageItems(items, state);
        items.push({
            className: 'fasting-timer-card__remaining fasting-timer-card__remaining--done fd-ui-body-sm',
            text: this.translate('FASTING.COMPLETED'),
        });
        this.pushOptionalTranslatedItem(items, 'fasting-timer-card__next-stage-label', state.stageDescriptionKey);
    }

    private buildSummaryRingItems(): FastingTimerCardDisplayItem[] {
        const state = this.viewState();
        if (!state.isActive && !state.currentSessionCompleted) {
            return [
                {
                    className: 'fasting-timer-card__elapsed fasting-timer-card__elapsed--idle fd-ui-metric-lg',
                    text: this.translate('FASTING.READY'),
                },
                { className: 'fasting-timer-card__remaining fd-ui-body-sm', text: this.translate('FASTING.SELECT_AND_START') },
            ];
        }

        const items: FastingTimerCardDisplayItem[] = [];
        this.pushOptionalItem(items, 'fasting-timer-card__state fasting-timer-card__state--summary fd-ui-body-sm', state.stateLabel);
        items.push({
            className: 'fasting-timer-card__elapsed fasting-timer-card__elapsed--summary fd-ui-metric-lg',
            text: state.elapsedFormatted,
        });
        items.push({
            className: 'fasting-timer-card__percent fasting-timer-card__percent--summary-secondary fd-ui-stat-value',
            text: `${this.normalizedProgressPercent().toFixed(0)}%`,
        });

        return items;
    }

    private buildSummaryContentGroups(): FastingTimerCardDisplayGroup[] {
        const state = this.viewState();
        if (state.isActive) {
            return this.buildActiveSummaryGroups(state);
        }

        if (state.currentSessionCompleted) {
            return this.buildCompletedSummaryGroups(state);
        }

        return [
            {
                className: 'fasting-timer-card__summary-time fasting-timer-card__summary-time--idle fd-stack fd-gap-card-header',
                items: [
                    { className: 'fd-ui-metric-lg', text: this.translate('FASTING.READY') },
                    { className: 'fasting-timer-card__remaining fd-ui-body-sm', text: this.translate('FASTING.SELECT_AND_START') },
                ],
            },
        ];
    }

    private buildActiveSummaryGroups(state: FastingTimerCardState): FastingTimerCardDisplayGroup[] {
        const groups: FastingTimerCardDisplayGroup[] = [];

        if (this.shouldShowStageProgress()) {
            const stageItems: FastingTimerCardDisplayItem[] = [];
            if (state.metaLabel === null) {
                stageItems.push({
                    className: 'fasting-timer-card__stage-progress fd-ui-overline',
                    text: this.getStageProgressText(state),
                });
            }

            this.pushOptionalTranslatedItem(stageItems, 'fasting-timer-card__stage-title fd-ui-card-title', state.stageTitleKey);
            this.pushOptionalTranslatedItem(stageItems, 'fasting-timer-card__next-stage-label', state.stageDescriptionKey);
            groups.push({ className: 'fasting-timer-card__summary-stage fd-stack fd-gap-card-header', items: stageItems });
        }

        if (!this.isEatingPhase() && !state.isOvertime && state.nextStageTitleKey !== null && state.nextStageFormatted !== null) {
            groups.push({
                className: 'fasting-timer-card__summary-next fd-stack fd-gap-card-header',
                items: [
                    {
                        className: 'fasting-timer-card__next-stage-time fd-ui-body-sm',
                        text: this.translate('FASTING.STAGES.NEXT_IN', { time: state.nextStageFormatted }),
                    },
                ],
            });
        }

        groups.push({
            className: 'fasting-timer-card__summary-meta fd-row fd-gap-card-header',
            items: [
                {
                    className: state.isOvertime
                        ? 'fasting-timer-card__remaining fasting-timer-card__remaining--done fd-ui-body-sm'
                        : 'fasting-timer-card__remaining fd-ui-body-sm',
                    text: this.getRemainingText(state),
                },
            ],
        });

        if (this.shouldShowStageDescriptionFallback() && state.stageDescriptionKey !== null) {
            groups.push({
                className: 'fasting-timer-card__summary-description fd-stack fd-gap-card-header',
                items: [{ className: 'fasting-timer-card__next-stage-label', text: this.translate(state.stageDescriptionKey) }],
            });
        }

        return groups;
    }

    private buildCompletedSummaryGroups(state: FastingTimerCardState): FastingTimerCardDisplayGroup[] {
        const groups: FastingTimerCardDisplayGroup[] = [];
        if (this.shouldShowStageProgress()) {
            const stageItems: FastingTimerCardDisplayItem[] = [];
            this.addStageItems(stageItems, state);
            groups.push({ className: 'fasting-timer-card__summary-stage fd-stack fd-gap-card-header', items: stageItems });
        }

        groups.push({
            className: 'fasting-timer-card__summary-completed fd-stack fd-gap-card-header',
            items: [
                {
                    className: 'fasting-timer-card__remaining fasting-timer-card__remaining--done fd-ui-body-sm',
                    text: this.translate('FASTING.COMPLETED'),
                },
                ...this.buildOptionalTranslatedItems('fasting-timer-card__next-stage-label', state.stageDescriptionKey),
            ],
        });

        return groups;
    }

    private addStageItems(items: FastingTimerCardDisplayItem[], state: FastingTimerCardState): void {
        if (!this.shouldShowStageProgress()) {
            return;
        }

        items.push({ className: 'fasting-timer-card__stage-progress fd-ui-overline', text: this.getStageProgressText(state) });
        this.pushOptionalTranslatedItem(items, 'fasting-timer-card__stage-title fd-ui-card-title', state.stageTitleKey);
    }

    private addStageDescriptionFallbackItem(items: FastingTimerCardDisplayItem[], state: FastingTimerCardState): void {
        if (!this.shouldShowStageDescriptionFallback()) {
            return;
        }

        this.pushOptionalTranslatedItem(items, 'fasting-timer-card__next-stage-label', state.stageDescriptionKey);
    }

    private getRemainingText(state: FastingTimerCardState): string {
        return state.isOvertime
            ? this.translate('FASTING.GOAL_REACHED')
            : `${this.translate(state.remainingLabelKey)}: ${state.remainingFormatted}`;
    }

    private getStageProgressText(state: FastingTimerCardState): string {
        return this.translate('FASTING.STAGES.PROGRESS', { current: state.stageIndex, total: state.totalStages });
    }

    private pushOptionalItem(items: FastingTimerCardDisplayItem[], className: string, text: string | null): void {
        if (text !== null) {
            items.push({ className, text });
        }
    }

    private pushOptionalTranslatedItem(items: FastingTimerCardDisplayItem[], className: string, translationKey: string | null): void {
        items.push(...this.buildOptionalTranslatedItems(className, translationKey));
    }

    private buildOptionalTranslatedItems(className: string, translationKey: string | null): FastingTimerCardDisplayItem[] {
        return translationKey !== null ? [{ className, text: this.translate(translationKey) }] : [];
    }

    private buildStageViewState(
        state: ReturnType<typeof buildFastingTimerCardComputedState>,
    ): Pick<
        FastingTimerCardState,
        'glowColor' | 'stageTitleKey' | 'stageDescriptionKey' | 'stageIndex' | 'totalStages' | 'nextStageTitleKey' | 'nextStageFormatted'
    > {
        const stage = state.stage;
        if (stage === null) {
            return {
                glowColor: null,
                stageTitleKey: null,
                stageDescriptionKey: null,
                stageIndex: null,
                totalStages: EMPTY_DURATION_MS,
                nextStageTitleKey: null,
                nextStageFormatted: state.nextStageFormatted,
            };
        }

        return {
            glowColor: stage.glowColor,
            stageTitleKey: stage.titleKey,
            stageDescriptionKey: stage.descriptionKey,
            stageIndex: state.showStageProgress ? stage.index : null,
            totalStages: state.showStageProgress ? stage.total : EMPTY_DURATION_MS,
            nextStageTitleKey: stage.nextTitleKey ?? null,
            nextStageFormatted: state.nextStageFormatted,
        };
    }

    private getSession(): FastingSession | null {
        if (this.layout() === 'page' && this.facade !== null) {
            return this.facade.currentSession();
        }

        return this.session();
    }

    private getElapsedMs(): number {
        if (this.usesFacadeTimer() && this.facade !== null) {
            return this.facade.elapsedMs();
        }

        const session = this.getSession();
        if (session === null) {
            return EMPTY_DURATION_MS;
        }

        const start = new Date(session.startedAtUtc).getTime();
        const end = session.endedAtUtc !== null ? new Date(session.endedAtUtc).getTime() : this.now().getTime();
        if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) {
            return EMPTY_DURATION_MS;
        }

        return end - start;
    }

    private getCardLabelKey(session: FastingSession | null): string {
        if (session === null) {
            return 'FASTING.WIDGET_LABEL';
        }

        switch (session.planType) {
            case 'Cyclic':
                return 'FASTING.CYCLIC_TYPE';
            case 'Extended':
                return 'FASTING.EXTENDED_TYPE';
            case 'Intermittent':
                return 'FASTING.INTERMITTENT_TYPE';
        }
    }

    private startTimer(): void {
        if (this.timerInterval !== null) {
            return;
        }

        this.now.set(new Date());
        this.timerInterval = setInterval(() => {
            this.now.set(new Date());
        }, TIMER_TICK_MS);
    }

    private stopTimer(): void {
        if (this.timerInterval === null) {
            return;
        }

        clearInterval(this.timerInterval);
        this.timerInterval = null;
    }
}

interface FastingTimerCardChrome {
    density: 'compact' | 'default';
    title: string;
    showPageControls: boolean;
}
