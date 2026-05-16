import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { PERCENT_MULTIPLIER as PERCENT_MAX } from '../../../shared/lib/nutrition.constants';
import { DashboardWidgetFrameComponent } from '../dashboard-widget-frame/dashboard-widget-frame.component';
import { NoticeBannerComponent } from '../notice-banner/notice-banner.component';
import type { NutrientBar, NutrientBarViewModel } from './dashboard-summary-card.types';
import {
    buildDefaultDashboardNutrientBars,
    buildRingDasharray,
    calculateDashboardPercent,
    clampDashboardPercent,
    getDashboardColorForPercent,
    getDashboardNutrientBarColor,
    mixDashboardColorWithWhite,
    normalizeDailyGoal,
    normalizeWeeklyGoal,
} from './dashboard-summary-card.utils';

const OUTER_RING_RADIUS = 112;
const INNER_RING_RADIUS = 88;
const RANDOM_ID_RADIX = 36;
const RANDOM_ID_START = 2;
const RANDOM_ID_END = 9;
const GRADIENT_START_WHITE_MIX = 0.05;
const GRADIENT_END_WHITE_MIX = 0.15;
const ANIMATION_MS_PER_PERCENT = 10;

@Component({
    selector: 'fd-dashboard-summary-card',
    imports: [CommonModule, TranslatePipe, NoticeBannerComponent, DashboardWidgetFrameComponent],
    templateUrl: './dashboard-summary-card.component.html',
    styleUrl: './dashboard-summary-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardSummaryCardComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    public readonly goalAction = output();
    public readonly dailyGoal = input.required<number>();
    public readonly dailyConsumed = input.required<number>();
    public readonly weeklyConsumed = input.required<number>();
    public readonly weeklyGoal = input.required<number | null>();
    public readonly nutrientBars = input.required<NutrientBar[] | null>();
    public readonly caloriesBurned = input<number>(0);
    public readonly isDailyHovered = signal(false);
    public readonly isWeeklyHovered = signal(false);
    private readonly outerRadius = OUTER_RING_RADIUS;
    private readonly innerRadius = INNER_RING_RADIUS;
    public readonly dailyCircumference = 2 * Math.PI * this.outerRadius;
    public readonly weeklyCircumference = 2 * Math.PI * this.innerRadius;
    private readonly gradientIdDaily = `consumption-ring-daily-${this.createRandomIdPart()}`;
    private readonly gradientIdWeekly = `consumption-ring-weekly-${this.createRandomIdPart()}`;
    public readonly normalizedDailyGoal = computed(() => normalizeDailyGoal(this.dailyGoal()));
    public readonly normalizedWeeklyGoal = computed(() => normalizeWeeklyGoal(this.weeklyGoal(), this.normalizedDailyGoal()));

    public readonly safeWeeklyConsumed = computed(() => Math.max(this.weeklyConsumed(), 0));

    public readonly dailyPercent = computed(() => calculateDashboardPercent(this.dailyConsumed(), this.normalizedDailyGoal()));
    public readonly weeklyPercent = computed(() => calculateDashboardPercent(this.safeWeeklyConsumed(), this.normalizedWeeklyGoal()));

    private readonly animatedDailyPercent = signal(0);
    private readonly animatedWeeklyPercent = signal(0);
    private readonly colorCache = new Map<string, [number, number, number]>();

    public readonly dailyDasharray = computed(() => buildRingDasharray(this.animatedDailyPercent(), this.outerRadius));
    public readonly weeklyDasharray = computed(() => buildRingDasharray(this.animatedWeeklyPercent(), this.innerRadius));
    public readonly dailyStrokeColor = computed(() => getDashboardColorForPercent(this.animatedDailyPercent(), this.colorCache));
    public readonly weeklyStrokeColor = computed(() => getDashboardColorForPercent(this.animatedWeeklyPercent(), this.colorCache));
    public readonly dailyGradientStart = computed(() =>
        mixDashboardColorWithWhite(this.dailyStrokeColor(), GRADIENT_START_WHITE_MIX, this.colorCache),
    );
    public readonly dailyGradientEnd = computed(() =>
        mixDashboardColorWithWhite(this.dailyStrokeColor(), GRADIENT_END_WHITE_MIX, this.colorCache),
    );
    public readonly weeklyGradientStart = computed(() =>
        mixDashboardColorWithWhite(this.weeklyStrokeColor(), GRADIENT_START_WHITE_MIX, this.colorCache),
    );
    public readonly weeklyGradientEnd = computed(() =>
        mixDashboardColorWithWhite(this.weeklyStrokeColor(), GRADIENT_END_WHITE_MIX, this.colorCache),
    );
    public readonly resolvedNutrientBars = computed(() => this.nutrientBars() ?? buildDefaultDashboardNutrientBars());
    public readonly nutrientBarViewModels = computed<NutrientBarViewModel[]>(() =>
        this.resolvedNutrientBars().map(bar => {
            this.languageVersion();

            return {
                ...bar,
                labelText: bar.labelKey !== undefined && bar.labelKey.length > 0 ? this.translateService.instant(bar.labelKey) : bar.label,
                unitText: bar.unitKey !== undefined && bar.unitKey.length > 0 ? this.translateService.instant(bar.unitKey) : bar.unit,
                valueColor: getDashboardNutrientBarColor(bar, this.colorCache),
                fillBackground:
                    bar.target > 0
                        ? `linear-gradient(90deg, ${bar.colorStart} 0%, ${bar.colorEnd} ${PERCENT_MAX}%)`
                        : 'var(--fd-color-slate-300)',
                fillWidth: bar.target > 0 ? clampDashboardPercent((bar.current / bar.target) * PERCENT_MAX) : PERCENT_MAX,
            };
        }),
    );
    private readonly hasCalorieGoal = computed(() => this.normalizedDailyGoal() > 0);
    private readonly hasMacroGoals = computed(() => (this.nutrientBars() ?? []).some(bar => bar.target > 0));
    public readonly showNotice = computed(() => !this.hasCalorieGoal() || !this.hasMacroGoals());
    public readonly noticeVariant = computed(() => {
        const hasCalories = this.hasCalorieGoal();
        const hasMacros = this.hasMacroGoals();

        if (!hasCalories && !hasMacros) {
            return 'none';
        }
        if (hasCalories && !hasMacros) {
            return 'macros';
        }
        if (!hasCalories && hasMacros) {
            return 'calories';
        }
        return 'ok';
    });
    public readonly noticeTitleKey = computed(() => {
        switch (this.noticeVariant()) {
            case 'none':
                return 'DASHBOARD_SUMMARY.GOALS_TITLE';
            case 'macros':
                return 'DASHBOARD_SUMMARY.MACROS_TITLE';
            case 'calories':
            case 'ok':
                return 'DASHBOARD_SUMMARY.CALORIES_TITLE';
        }
    });
    public readonly noticeMessageKey = computed(() => {
        switch (this.noticeVariant()) {
            case 'none':
                return 'DASHBOARD_SUMMARY.GOALS_BODY';
            case 'macros':
                return 'DASHBOARD_SUMMARY.MACROS_BODY';
            case 'calories':
            case 'ok':
                return 'DASHBOARD_SUMMARY.CALORIES_BODY';
        }
    });
    public readonly dailyGradientId = this.gradientIdDaily;
    public readonly weeklyGradientId = this.gradientIdWeekly;
    public readonly dailyGradientStroke = `url(#${this.gradientIdDaily})`;
    public readonly weeklyGradientStroke = `url(#${this.gradientIdWeekly})`;

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });

        effect(onCleanup => {
            const target = this.dailyPercent();
            this.startAnimation(this.animatedDailyPercent, target);
            onCleanup(() => {
                this.stopAnimation(this.animatedDailyPercent);
            });
        });

        effect(onCleanup => {
            const target = this.weeklyPercent();
            this.startAnimation(this.animatedWeeklyPercent, target);
            onCleanup(() => {
                this.stopAnimation(this.animatedWeeklyPercent);
            });
        });
    }

    private createRandomIdPart(): string {
        return Math.random().toString(RANDOM_ID_RADIX).slice(RANDOM_ID_START, RANDOM_ID_END);
    }

    public clampPercent(value: number): number {
        return clampDashboardPercent(value);
    }

    private startAnimation(targetSignal: { set: (v: number) => void; (): number }, target: number): void {
        this.stopAnimation(targetSignal);
        targetSignal.set(0);

        const startTime = performance.now();
        const duration = Math.max(target, 1) * ANIMATION_MS_PER_PERCENT;

        const step = (): void => {
            const elapsed = performance.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const current = target * progress;
            targetSignal.set(current);

            if (progress < 1) {
                const handle = requestAnimationFrame(step);
                this.animationHandles.set(targetSignal, handle);
            }
        };

        const handle = requestAnimationFrame(step);
        this.animationHandles.set(targetSignal, handle);
    }

    private stopAnimation(targetSignal: { (): number }): void {
        const handle = this.animationHandles.get(targetSignal);
        if (handle !== undefined) {
            cancelAnimationFrame(handle);
            this.animationHandles.delete(targetSignal);
        }
    }

    public setDailyHover(state: boolean): void {
        if (this.normalizedDailyGoal() <= 0) {
            this.isDailyHovered.set(false);
            return;
        }
        this.isDailyHovered.set(state);
    }

    public setWeeklyHover(state: boolean): void {
        if (this.normalizedDailyGoal() <= 0) {
            this.isWeeklyHovered.set(false);
            return;
        }
        this.isWeeklyHovered.set(state);
    }

    public onGoalAction(): void {
        this.goalAction.emit();
    }

    private readonly animationHandles = new Map<object, number>();
}
