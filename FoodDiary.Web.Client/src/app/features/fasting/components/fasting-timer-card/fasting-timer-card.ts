import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { MS_PER_SECOND } from '../../../../shared/lib/time.constants';
import { FastingFacade } from '../../lib/fasting.facade';
import { buildFastingTimerCardComputedState } from '../../lib/fasting-timer-card-state';
import type { FastingSession } from '../../models/fasting.data';
import { FastingControlsComponent } from '../fasting-controls/fasting-controls';
import { FastingTimerCardGroupsComponent } from './fasting-timer-card-groups/fasting-timer-card-groups';
import { FastingTimerCardItemsComponent } from './fasting-timer-card-items/fasting-timer-card-items';
import type {
    FastingTimerCardChrome,
    FastingTimerCardLayout,
    FastingTimerCardState,
} from './fasting-timer-card-lib/fasting-timer-card.types';
import {
    buildFastingMainRingItems,
    buildFastingSummaryContentGroups,
    buildFastingSummaryRingItems,
    type FastingTimerCardDisplayContext,
} from './fasting-timer-card-lib/fasting-timer-card-display.mapper';

const RING_RADIUS = 90;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;
const EMPTY_DURATION_MS = 0;

@Component({
    selector: 'fd-fasting-timer-card',
    imports: [
        NgTemplateOutlet,
        TranslatePipe,
        FdUiCardComponent,
        DashboardWidgetFrameComponent,
        FastingControlsComponent,
        FastingTimerCardItemsComponent,
        FastingTimerCardGroupsComponent,
    ],
    templateUrl: './fasting-timer-card.html',
    styleUrl: './fasting-timer-card.scss',
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
    protected readonly normalizedProgressPercent = computed(() => {
        const progress = this.viewState().progressPercent;
        return Number.isFinite(progress) ? Math.max(EMPTY_DURATION_MS, Math.min(progress, PERCENT_MULTIPLIER)) : EMPTY_DURATION_MS;
    });
    protected readonly ringStrokeDasharray = RING_CIRCUMFERENCE;
    protected readonly ringStrokeDashoffset = computed(
        () => RING_CIRCUMFERENCE * (1 - this.normalizedProgressPercent() / PERCENT_MULTIPLIER),
    );
    public readonly layout = input<FastingTimerCardLayout>('page');
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
    protected readonly mainRingItems = computed(() => buildFastingMainRingItems(this.buildDisplayContext()));
    protected readonly summaryRingItems = computed(() => buildFastingSummaryRingItems(this.buildDisplayContext()));
    protected readonly summaryContentGroups = computed(() => buildFastingSummaryContentGroups(this.buildDisplayContext()));
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
        effect(() => {
            const session = this.getSession();
            if (session !== null && session.endedAtUtc === null && !this.usesFacadeTimer()) {
                this.startTimer();
                return;
            }

            this.stopTimer();
        });

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

    private buildDisplayContext(): FastingTimerCardDisplayContext {
        return {
            state: this.viewState(),
            isSetupLayout: this.isSetupLayout(),
            isEatingPhase: this.isEatingPhase(),
            showStageProgress: this.shouldShowStageProgress(),
            showStageDescriptionFallback: this.shouldShowStageDescriptionFallback(),
            normalizedProgressPercent: this.normalizedProgressPercent(),
            translate: this.translate,
        };
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
            case 'Cyclic': {
                return 'FASTING.CYCLIC_TYPE';
            }
            case 'Extended': {
                return 'FASTING.EXTENDED_TYPE';
            }
            case 'Intermittent': {
                return 'FASTING.INTERMITTENT_TYPE';
            }
        }
    }

    private startTimer(): void {
        if (this.timerInterval !== null) {
            return;
        }

        this.now.set(new Date());
        this.timerInterval = setInterval(() => {
            this.now.set(new Date());
        }, MS_PER_SECOND);
    }

    private stopTimer(): void {
        if (this.timerInterval === null) {
            return;
        }

        clearInterval(this.timerInterval);
        this.timerInterval = null;
    }
}
