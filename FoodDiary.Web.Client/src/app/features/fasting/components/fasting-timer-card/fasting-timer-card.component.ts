import { DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame.component';
import { LocalizationService } from '../../../../services/localization.service';
import { FastingFacade } from '../../lib/fasting.facade';
import { buildFastingTimerCardComputedState } from '../../lib/fasting-timer-card-state';
import { type FastingOccurrenceKind, type FastingSession } from '../../models/fasting.data';
import { FastingControlsComponent } from '../fasting-controls/fasting-controls.component';

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

@Component({
    selector: 'fd-fasting-timer-card',
    standalone: true,
    imports: [DecimalPipe, NgTemplateOutlet, TranslatePipe, FdUiCardComponent, DashboardWidgetFrameComponent, FastingControlsComponent],
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
        if (!this.usesFacadeTimer() && session && !session.endedAtUtc) {
            this.startTimer();
            return;
        }

        this.stopTimer();
    });
    protected readonly normalizedProgressPercent = computed(() => {
        const progress = this.viewState().progressPercent;
        return Number.isFinite(progress) ? Math.max(0, Math.min(progress, 100)) : 0;
    });
    protected readonly ringStrokeDasharray = RING_CIRCUMFERENCE;
    protected readonly ringStrokeDashoffset = computed(() => RING_CIRCUMFERENCE * (1 - this.normalizedProgressPercent() / 100));
    public readonly layout = input<'dashboard' | 'page'>('page');
    public readonly session = input<FastingSession | null>(null);
    private readonly usesFacadeTimer = computed(() => this.layout() === 'page' && this.facade !== null);
    protected readonly viewState = computed<FastingTimerCardState>(() => {
        this.currentLanguage();
        const session = this.getSession();
        const isActive = !!session && !session.endedAtUtc;
        const state = buildFastingTimerCardComputedState({
            session,
            elapsedMs: this.getElapsedMs(),
            translate: this.translate,
        });
        const stage = state.stage;

        return {
            isActive,
            isOvertime: state.isOvertime,
            currentSessionCompleted: !!session?.endedAtUtc,
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
            glowColor: stage?.glowColor ?? null,
            stageTitleKey: stage?.titleKey ?? null,
            stageDescriptionKey: stage?.descriptionKey ?? null,
            stageIndex: state.showStageProgress ? (stage?.index ?? null) : null,
            totalStages: state.showStageProgress ? (stage?.total ?? 4) : 0,
            nextStageTitleKey: stage?.nextTitleKey ?? null,
            nextStageFormatted: state.nextStageFormatted,
            showGlow: this.layout() === 'page' && !isActive,
        };
    });

    protected readonly isDashboardLayout = computed(() => this.layout() === 'dashboard');
    protected readonly isSetupLayout = computed(() => this.layout() === 'page' && !this.viewState().isActive);
    protected readonly isPageSummaryLayout = computed(() => this.layout() === 'page' && this.viewState().isActive);
    protected readonly cardChrome = computed<FastingTimerCardChrome>(() => ({
        density: this.isDashboardLayout() || this.isPageSummaryLayout() ? 'compact' : 'default',
        titleKey: this.layout() === 'page' ? 'FASTING.TITLE' : null,
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
        if (!glowColor) {
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

    private getSession(): FastingSession | null {
        if (this.layout() === 'page' && this.facade) {
            return this.facade.currentSession();
        }

        return this.session();
    }

    private getElapsedMs(): number {
        if (this.usesFacadeTimer() && this.facade) {
            return this.facade.elapsedMs();
        }

        const session = this.getSession();
        if (!session) {
            return 0;
        }

        const start = new Date(session.startedAtUtc).getTime();
        const end = session.endedAtUtc ? new Date(session.endedAtUtc).getTime() : this.now().getTime();
        if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) {
            return 0;
        }

        return end - start;
    }

    private getCardLabelKey(session: FastingSession | null): string {
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

    private startTimer(): void {
        if (this.timerInterval !== null) {
            return;
        }

        this.now.set(new Date());
        this.timerInterval = setInterval(() => {
            this.now.set(new Date());
        }, 1000);
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
    titleKey: string | null;
    showPageControls: boolean;
}
