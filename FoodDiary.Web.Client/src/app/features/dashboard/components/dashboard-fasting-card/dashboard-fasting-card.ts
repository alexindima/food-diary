import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { MS_PER_SECOND } from '../../../../shared/lib/time.constants';
import { buildFastingTimerCardComputedState } from '../../../fasting/lib/fasting-timer-card-state';
import type { FastingSession } from '../../../fasting/models/fasting.data';
import { buildDashboardFastingCycle, buildDashboardFastingDayTicks, buildDashboardFastingTimeline } from './dashboard-fasting-timeline';

const EMPTY_DURATION_MS = 0;

@Component({
    selector: 'fd-dashboard-fasting-card',
    imports: [TranslatePipe, FdUiIconComponent, DashboardWidgetFrameComponent],
    templateUrl: './dashboard-fasting-card.html',
    styleUrl: './dashboard-fasting-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardFastingCardComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly localizationService = inject(LocalizationService);
    private readonly translateService = inject(TranslateService);
    private readonly now = signal(new Date());
    private readonly currentLanguage = signal(this.localizationService.getCurrentLanguage());
    private timerInterval: ReturnType<typeof setInterval> | null = null;

    public readonly session = input.required<FastingSession | null>();
    protected readonly timeline = computed(() => buildDashboardFastingTimeline(this.session(), this.elapsedMs()));
    protected readonly dayTicks = computed(() => buildDashboardFastingDayTicks(this.timeline()));
    protected readonly fastFill = computed(() =>
        this.timeline().intermittent ? Math.min(this.timeline().position, this.timeline().boundary) : this.timeline().position,
    );
    protected readonly eatFill = computed(() =>
        this.timeline().intermittent ? Math.max(0, this.timeline().position - this.timeline().boundary) : 0,
    );
    protected readonly cycleDays = computed(() => buildDashboardFastingCycle(this.session()));
    protected readonly cycleProtocol = computed(() => `${this.session()?.cyclicFastDays ?? 1}:${this.session()?.cyclicEatDays ?? 1}`);
    protected readonly phaseIcon = computed(() => (this.timeline().eating ? 'restaurant' : 'bedtime'));
    protected readonly phaseLabelKey = computed(() => (this.timeline().eating ? 'FASTING.EATING_WINDOW' : 'FASTING.FASTING_WINDOW'));
    protected readonly stageTitleKey = computed(() => (this.timeline().eating ? 'FASTING.EATING_WINDOW' : this.fastingStageTitleKey()));
    protected readonly stageDescriptionKey = computed(() => {
        if (this.timeline().eating) {
            return 'FASTING.REDESIGN.STAGE_DESCRIPTION.EATING';
        }
        const titleKey = this.state().stage?.titleKey;
        return (
            titleKey?.replace('FASTING.STAGES.', 'FASTING.REDESIGN.STAGE_DESCRIPTION.').replace('.TITLE', '') ??
            'FASTING.REDESIGN.STAGE_DESCRIPTION.EARLY'
        );
    });
    protected readonly phaseDurationHours = computed(() => {
        const axis = this.timeline();
        return axis.intermittent ? (axis.eating ? axis.eatHours : axis.fastHours) : (this.session()?.plannedDurationHours ?? 0);
    });
    protected readonly stageSummary = computed(() => {
        this.currentLanguage();
        const view = this.state();
        if (view.isOvertime) {
            return this.translateService.instant('FASTING.REDESIGN.PAST_TARGET');
        }
        if (!this.timeline().eating && view.nextStageFormatted !== null) {
            return this.translateService.instant('FASTING.STAGES.NEXT_IN', { time: view.nextStageFormatted });
        }
        return `${this.translateService.instant(view.remainingLabelKey)}: ${view.remainingFormatted}`;
    });

    private fastingStageTitleKey(): string {
        const key = this.state().stage?.titleKey;
        return key === 'FASTING.STAGES.STORED_ENERGY.TITLE' ? 'FASTING.REDESIGN.STORED_ENERGY_SHORT' : (key ?? 'FASTING.REDESIGN.READY');
    }

    protected formatTime(date: Date | null): string {
        return date === null
            ? '—'
            : new Intl.DateTimeFormat(this.currentLanguage(), { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).format(date);
    }

    protected formatDate(date: Date | null): string {
        return date === null ? '' : new Intl.DateTimeFormat(this.currentLanguage(), { day: 'numeric', month: 'short' }).format(date);
    }

    protected readonly state = computed(() => {
        this.currentLanguage();
        return buildFastingTimerCardComputedState({
            session: this.session(),
            elapsedMs: this.elapsedMs(),
            translate: (key, params) => this.translateService.instant(key, params),
        });
    });
    protected readonly progress = computed(() => Math.min(PERCENT_MULTIPLIER, Math.max(EMPTY_DURATION_MS, this.state().progressPercent)));
    protected readonly stageColor = computed(() => {
        const state = this.state();
        if (state.isOvertime || this.timeline().eating) {
            return 'var(--fd-color-green-500)';
        }

        return state.ringColor ?? 'var(--fd-color-primary-500)';
    });

    public constructor() {
        effect(() => {
            const session = this.session();
            if (session !== null && session.endedAtUtc === null) {
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

    private elapsedMs(): number {
        const session = this.session();
        if (session === null) {
            return EMPTY_DURATION_MS;
        }

        const start = new Date(session.startedAtUtc).getTime();
        const end = session.endedAtUtc === null ? this.now().getTime() : new Date(session.endedAtUtc).getTime();
        if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) {
            return EMPTY_DURATION_MS;
        }

        return end - start;
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
