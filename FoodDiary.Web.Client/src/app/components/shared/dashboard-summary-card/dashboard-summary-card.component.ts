import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { PERCENT_MULTIPLIER as PERCENT_MAX } from '../../../shared/lib/nutrition.constants';
import { DashboardWidgetFrameComponent } from '../dashboard-widget-frame/dashboard-widget-frame.component';
import { NoticeBannerComponent } from '../notice-banner/notice-banner.component';

const COLOR_FALLBACK = '#5aa9fa';
const OUTER_RING_RADIUS = 112;
const INNER_RING_RADIUS = 88;
const RANDOM_ID_RADIX = 36;
const RANDOM_ID_START = 2;
const RANDOM_ID_END = 9;
const WEEK_DAYS = 7;
const PROGRESS_CLAMP_MAX = 120;
const GRADIENT_START_WHITE_MIX = 0.05;
const GRADIENT_END_WHITE_MIX = 0.15;
const ANIMATION_MS_PER_PERCENT = 10;
const BLEND_HALF_WIDTH = 5;
const COLOR_SHORT_HEX_LENGTH = 3;
const COLOR_HEX_RADIX = 16;
const COLOR_RED_SHIFT = 16;
const COLOR_GREEN_SHIFT = 8;
const COLOR_BYTE_MASK = 0xff;
const RGB_CHANNEL_COUNT = 3;
const WHITE_CHANNEL = 255;
const DEFAULT_PROTEIN_CURRENT = 110;
const DEFAULT_PROTEIN_TARGET = 140;
const DEFAULT_CARBS_CURRENT = 180;
const DEFAULT_CARBS_TARGET = 250;
const DEFAULT_FATS_CURRENT = 45;
const DEFAULT_FATS_TARGET = 70;
const DEFAULT_FIBER_CURRENT = 18;
const DEFAULT_FIBER_TARGET = 30;

export type NutrientBar = {
    id: string;
    label: string;
    labelKey?: string;
    current: number;
    target: number;
    unit: string;
    unitKey?: string;
    colorStart: string;
    colorEnd: string;
};

@Component({
    selector: 'fd-dashboard-summary-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, NoticeBannerComponent, DashboardWidgetFrameComponent],
    templateUrl: './dashboard-summary-card.component.html',
    styleUrl: './dashboard-summary-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardSummaryCardComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    private static readonly CSS_VAR_PATTERN = /^var\((--[^),\s]+)(?:,\s*([^)]+))?\)$/;
    private static readonly CSS_COLOR_VALUES: Partial<Record<string, string>> = {
        '--fd-color-sky-500': '#0ea5e9',
        '--fd-color-blue-500': '#3b82f6',
        '--fd-color-emerald-500': '#10b981',
        '--fd-color-green-500': '#22c55e',
        '--fd-color-emerald-700': '#047857',
        '--fd-color-amber-500': '#f59e0b',
        '--fd-color-orange-500': '#f97316',
        '--fd-color-danger': '#ef4444',
    };
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
    private readonly colorStops = [
        { percent: 0, color: 'var(--fd-color-sky-500)' },
        { percent: 50, color: 'var(--fd-color-blue-500)' },
        { percent: 70, color: 'var(--fd-color-blue-500)' },
        { percent: 80, color: 'var(--fd-color-emerald-500)' },
        { percent: 90, color: 'var(--fd-color-green-500)' },
        { percent: 100, color: 'var(--fd-color-emerald-700)' },
        { percent: 110, color: 'var(--fd-color-amber-500)' },
        { percent: 120, color: 'var(--fd-color-orange-500)' },
        { percent: 130, color: 'var(--fd-color-danger)' },
    ];

    public readonly normalizedDailyGoal = computed(() => Math.max(this.dailyGoal(), 0));
    public readonly normalizedWeeklyGoal = computed(() => {
        const explicitWeeklyGoal = this.weeklyGoal();
        if (explicitWeeklyGoal !== null && explicitWeeklyGoal > 0) {
            return explicitWeeklyGoal;
        }

        const dailyGoal = this.normalizedDailyGoal();
        return dailyGoal > 0 ? dailyGoal * WEEK_DAYS : 0;
    });

    public readonly safeWeeklyConsumed = computed(() => Math.max(this.weeklyConsumed(), 0));

    public readonly dailyPercent = computed(() => this.calculatePercent(this.dailyConsumed(), this.normalizedDailyGoal()));
    public readonly weeklyPercent = computed(() => this.calculatePercent(this.safeWeeklyConsumed(), this.normalizedWeeklyGoal()));

    private readonly animatedDailyPercent = signal(0);
    private readonly animatedWeeklyPercent = signal(0);
    private readonly colorCache = new Map<string, [number, number, number]>();

    public readonly dailyDasharray = computed(() => this.buildDasharray(this.animatedDailyPercent(), this.outerRadius));
    public readonly weeklyDasharray = computed(() => this.buildDasharray(this.animatedWeeklyPercent(), this.innerRadius));
    public readonly dailyStrokeColor = computed(() => this.getColorForPercent(this.animatedDailyPercent()));
    public readonly weeklyStrokeColor = computed(() => this.getColorForPercent(this.animatedWeeklyPercent()));
    public readonly dailyGradientStart = computed(() => this.mixWithWhite(this.dailyStrokeColor(), GRADIENT_START_WHITE_MIX));
    public readonly dailyGradientEnd = computed(() => this.mixWithWhite(this.dailyStrokeColor(), GRADIENT_END_WHITE_MIX));
    public readonly weeklyGradientStart = computed(() => this.mixWithWhite(this.weeklyStrokeColor(), GRADIENT_START_WHITE_MIX));
    public readonly weeklyGradientEnd = computed(() => this.mixWithWhite(this.weeklyStrokeColor(), GRADIENT_END_WHITE_MIX));
    public readonly resolvedNutrientBars = computed(() => this.nutrientBars() ?? this.buildDefaultNutrientBars());
    public readonly nutrientBarViewModels = computed<NutrientBarViewModel[]>(() =>
        this.resolvedNutrientBars().map(bar => {
            this.languageVersion();

            return {
                ...bar,
                labelText: bar.labelKey !== undefined && bar.labelKey.length > 0 ? this.translateService.instant(bar.labelKey) : bar.label,
                unitText: bar.unitKey !== undefined && bar.unitKey.length > 0 ? this.translateService.instant(bar.unitKey) : bar.unit,
                valueColor: this.getBarColor(bar),
                fillBackground:
                    bar.target > 0
                        ? `linear-gradient(90deg, ${bar.colorStart} 0%, ${bar.colorEnd} ${PERCENT_MAX}%)`
                        : 'var(--fd-color-slate-300)',
                fillWidth: bar.target > 0 ? this.clampPercent((bar.current / bar.target) * PERCENT_MAX) : PERCENT_MAX,
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

    private calculatePercent(value: number, goal: number): number {
        if (goal <= 0) {
            return 0;
        }

        const normalized = Math.max(value, 0);
        return Math.round((normalized / goal) * PERCENT_MAX);
    }

    private buildDasharray(percent: number, radius: number): string {
        const circumference = 2 * Math.PI * radius;
        const clamped = Math.min(Math.max(percent, 0), PERCENT_MAX);
        const filled = (circumference * clamped) / PERCENT_MAX;
        return `${filled} ${circumference}`;
    }

    public clampPercent(value: number): number {
        if (Number.isNaN(value)) {
            return 0;
        }
        return Math.min(Math.max(value, 0), PROGRESS_CLAMP_MAX);
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

    private getColorForPercent(percent: number): string {
        const clamped = Math.max(percent, 0);
        const stops = this.colorStops;

        if (clamped <= stops[0].percent) {
            return stops[0].color;
        }
        if (clamped >= stops[stops.length - 1].percent) {
            return stops[stops.length - 1].color;
        }

        const blendHalfWidth = BLEND_HALF_WIDTH;

        for (let i = 1; i < stops.length; i += 1) {
            const prev = stops[i - 1];
            const curr = stops[i];
            const gap = curr.percent - prev.percent;
            const half = Math.min(blendHalfWidth, gap / 2);
            const blendStart = curr.percent - half;
            const blendEnd = curr.percent + half;

            if (clamped < blendStart) {
                return prev.color;
            }

            if (clamped <= blendEnd) {
                const t = half > 0 ? (clamped - blendStart) / (2 * half) : 1;
                return this.lerpColor(prev.color, curr.color, t);
            }
        }

        return stops[stops.length - 1].color;
    }

    private lerpColor(colorA: string, colorB: string, t: number): string {
        const [r1, g1, b1] = this.parseColor(colorA);
        const [r2, g2, b2] = this.parseColor(colorB);
        const lerp = (a: number, b: number): number => Math.round(a + (b - a) * t);
        const r = lerp(r1, r2);
        const g = lerp(g1, g2);
        const b = lerp(b1, b2);
        return `#${this.toHex(r)}${this.toHex(g)}${this.toHex(b)}`;
    }

    private hexToChannels(hex: string): [number, number, number] {
        const normalized = hex.replace('#', '');
        const value =
            normalized.length === COLOR_SHORT_HEX_LENGTH
                ? normalized
                      .split('')
                      .map(ch => ch + ch)
                      .join('')
                : normalized;
        const num = parseInt(value, COLOR_HEX_RADIX);
        const r = (num >> COLOR_RED_SHIFT) & COLOR_BYTE_MASK;
        const g = (num >> COLOR_GREEN_SHIFT) & COLOR_BYTE_MASK;
        const b = num & COLOR_BYTE_MASK;
        return [r, g, b];
    }

    private readonly animationHandles = new Map<object, number>();

    private toHex(value: number): string {
        return value.toString(COLOR_HEX_RADIX).padStart(2, '0');
    }

    private parseColor(value: string): [number, number, number] {
        const cached = this.colorCache.get(value);
        if (cached !== undefined) {
            return cached;
        }

        let channels: [number, number, number] | null = null;

        if (value.startsWith('#')) {
            channels = this.hexToChannels(value);
        } else {
            const cssVariable = DashboardSummaryCardComponent.CSS_VAR_PATTERN.exec(value.trim());
            if (cssVariable !== null) {
                channels = this.parseCssVariable(cssVariable[1], cssVariable[2]);
            } else {
                channels = this.parseRgbChannels(value);
            }
        }

        const resolved = channels ?? this.hexToChannels(COLOR_FALLBACK);
        this.colorCache.set(value, resolved);
        return resolved;
    }

    private parseCssVariable(variableName: string, fallback?: string): [number, number, number] | null {
        const colorValue = DashboardSummaryCardComponent.CSS_COLOR_VALUES[variableName];
        if (colorValue !== undefined) {
            return this.parseColor(colorValue);
        }

        return fallback !== undefined && fallback.length > 0 ? this.parseColor(fallback.trim()) : null;
    }

    private parseRgbChannels(value: string): [number, number, number] | null {
        const channels = value.match(/\d+/g)?.slice(0, RGB_CHANNEL_COUNT).map(Number);
        if (channels?.length === RGB_CHANNEL_COUNT) {
            return [channels[0], channels[1], channels[2]];
        }

        return null;
    }

    private mixWithWhite(color: string, ratio: number): string {
        const [r, g, b] = this.parseColor(color);
        const mix = (c: number): number => Math.round(c + (WHITE_CHANNEL - c) * ratio);
        return `#${this.toHex(mix(r))}${this.toHex(mix(g))}${this.toHex(mix(b))}`;
    }

    private buildDefaultNutrientBars(): NutrientBar[] {
        return [
            {
                id: 'protein',
                label: 'Protein',
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                current: DEFAULT_PROTEIN_CURRENT,
                target: DEFAULT_PROTEIN_TARGET,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-gradient-brand-start)',
                colorEnd: 'var(--fd-color-primary-600)',
            },
            {
                id: 'carbs',
                label: 'Carbs',
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                current: DEFAULT_CARBS_CURRENT,
                target: DEFAULT_CARBS_TARGET,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-teal-500)',
                colorEnd: 'var(--fd-color-sky-500)',
            },
            {
                id: 'fats',
                label: 'Fats',
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                current: DEFAULT_FATS_CURRENT,
                target: DEFAULT_FATS_TARGET,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-yellow-300)',
                colorEnd: 'var(--fd-color-orange-500)',
            },
            {
                id: 'fiber',
                label: 'Fiber',
                labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
                current: DEFAULT_FIBER_CURRENT,
                target: DEFAULT_FIBER_TARGET,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-rose-500)',
                colorEnd: 'var(--fd-color-rose-500)',
            },
        ];
    }

    private getBarColor(bar: NutrientBar): string {
        if (bar.target <= 0) {
            return 'var(--fd-color-gray-500-static)';
        }
        const pct = (bar.current / bar.target) * PERCENT_MAX;
        return this.getColorForPercent(pct);
    }
}

type NutrientBarViewModel = {
    labelText: string;
    unitText: string;
    valueColor: string;
    fillBackground: string;
    fillWidth: number;
} & NutrientBar;
