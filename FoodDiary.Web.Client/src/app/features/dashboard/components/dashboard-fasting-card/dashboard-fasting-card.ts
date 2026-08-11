import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiProgressRingComponent } from 'fd-ui-kit/progress-ring/fd-ui-progress-ring';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { MS_PER_SECOND } from '../../../../shared/lib/time.constants';
import { buildFastingTimerCardComputedState } from '../../../fasting/lib/fasting-timer-card-state';
import type { FastingSession } from '../../../fasting/models/fasting.data';

const EMPTY_DURATION_MS = 0;

@Component({
    selector: 'fd-dashboard-fasting-card',
    imports: [TranslatePipe, FdUiProgressRingComponent, DashboardWidgetFrameComponent],
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

    protected readonly state = computed(() => {
        this.currentLanguage();
        return buildFastingTimerCardComputedState({
            session: this.session(),
            elapsedMs: this.elapsedMs(),
            translate: (key, params) => this.translateService.instant(key, params),
        });
    });
    protected readonly progress = computed(() => Math.min(PERCENT_MULTIPLIER, Math.max(EMPTY_DURATION_MS, this.state().progressPercent)));
    protected readonly ringColor = computed(() => {
        const state = this.state();
        if (state.isOvertime) {
            return 'var(--fd-color-green-500)';
        }

        return state.ringColor ?? 'var(--fd-color-primary-500)';
    });
    protected readonly planTypeLabelKey = computed(() => {
        switch (this.session()?.planType) {
            case 'Cyclic': {
                return 'FASTING.CYCLIC_TYPE';
            }
            case 'Extended': {
                return 'FASTING.EXTENDED_TYPE';
            }
            case 'Intermittent':
            case undefined: {
                return 'FASTING.INTERMITTENT_TYPE';
            }
        }
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
