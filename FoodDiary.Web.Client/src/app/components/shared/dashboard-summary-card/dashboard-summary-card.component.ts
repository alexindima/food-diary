import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { NoticeBannerComponent } from '../notice-banner/notice-banner.component';
import { FdCardHoverDirective } from '../../../directives/card-hover.directive';

export interface NutrientBar {
    id: string;
    label: string;
    labelKey?: string;
    current: number;
    target: number;
    unit: string;
    unitKey?: string;
    colorStart: string;
    colorEnd: string;
}

@Component({
    selector: 'fd-dashboard-summary-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, NoticeBannerComponent, FdCardHoverDirective],
    templateUrl: './dashboard-summary-card.component.html',
    styleUrl: './dashboard-summary-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardSummaryCardComponent {
    private static readonly COLOR_FALLBACK_RGB: [number, number, number] = [90, 169, 250];
    public readonly goalAction = output<void>();
    public readonly dailyGoal = input<number>(0);
    public readonly dailyConsumed = input<number>(0);
    public readonly weeklyConsumed = input<number>(0);
    public readonly weeklyGoal = input<number | null>(null);
    public readonly nutrientBars = input<NutrientBar[] | null>(null);
    public readonly caloriesBurned = input<number>(0);
    public readonly isDailyHovered = signal(false);
    public readonly isWeeklyHovered = signal(false);
    private readonly outerRadius = 112;
    private readonly innerRadius = 88;
    public readonly dailyCircumference = 2 * Math.PI * this.outerRadius;
    public readonly weeklyCircumference = 2 * Math.PI * this.innerRadius;
    private readonly gradientIdDaily = `consumption-ring-daily-${Math.random().toString(36).slice(2, 9)}`;
    private readonly gradientIdWeekly = `consumption-ring-weekly-${Math.random().toString(36).slice(2, 9)}`;
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
        if (explicitWeeklyGoal && explicitWeeklyGoal > 0) {
            return explicitWeeklyGoal;
        }

        const dailyGoal = this.normalizedDailyGoal();
        return dailyGoal > 0 ? dailyGoal * 7 : 0;
    });

    public readonly safeWeeklyConsumed = computed(() => Math.max(this.weeklyConsumed(), 0));

    public readonly dailyPercent = computed(() => this.calculatePercent(this.dailyConsumed(), this.normalizedDailyGoal()));
    public readonly weeklyPercent = computed(() => this.calculatePercent(this.safeWeeklyConsumed(), this.normalizedWeeklyGoal()));

    private readonly animatedDailyPercent = signal(0);
    private readonly animatedWeeklyPercent = signal(0);

    public readonly dailyDasharray = computed(() => this.buildDasharray(this.animatedDailyPercent(), this.outerRadius));
    public readonly weeklyDasharray = computed(() => this.buildDasharray(this.animatedWeeklyPercent(), this.innerRadius));
    public readonly dailyStrokeColor = computed(() => this.getColorForPercent(this.animatedDailyPercent()));
    public readonly weeklyStrokeColor = computed(() => this.getColorForPercent(this.animatedWeeklyPercent()));
    public readonly dailyGradientStart = computed(() => this.mixWithWhite(this.dailyStrokeColor(), 0.05));
    public readonly dailyGradientEnd = computed(() => this.mixWithWhite(this.dailyStrokeColor(), 0.15));
    public readonly weeklyGradientStart = computed(() => this.mixWithWhite(this.weeklyStrokeColor(), 0.05));
    public readonly weeklyGradientEnd = computed(() => this.mixWithWhite(this.weeklyStrokeColor(), 0.15));
    public readonly resolvedNutrientBars = computed(() => this.nutrientBars() ?? this.buildDefaultNutrientBars());
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
            default:
                return 'DASHBOARD_SUMMARY.CALORIES_TITLE';
        }
    });
    public readonly noticeMessageKey = computed(() => {
        switch (this.noticeVariant()) {
            case 'none':
                return 'DASHBOARD_SUMMARY.GOALS_BODY';
            case 'macros':
                return 'DASHBOARD_SUMMARY.MACROS_BODY';
            default:
                return 'DASHBOARD_SUMMARY.CALORIES_BODY';
        }
    });
    public readonly dailyGradientId = this.gradientIdDaily;
    public readonly weeklyGradientId = this.gradientIdWeekly;

    public constructor() {
        effect(onCleanup => {
            const target = this.dailyPercent();
            this.startAnimation(this.animatedDailyPercent, target);
            onCleanup(() => this.stopAnimation(this.animatedDailyPercent));
        });

        effect(onCleanup => {
            const target = this.weeklyPercent();
            this.startAnimation(this.animatedWeeklyPercent, target);
            onCleanup(() => this.stopAnimation(this.animatedWeeklyPercent));
        });
    }

    private calculatePercent(value: number, goal: number): number {
        if (!goal || goal <= 0) {
            return 0;
        }

        const normalized = Math.max(value, 0);
        return Math.round((normalized / goal) * 100);
    }

    private buildDasharray(percent: number, radius: number): string {
        const circumference = 2 * Math.PI * radius;
        const clamped = Math.min(Math.max(percent, 0), 100);
        const filled = (circumference * clamped) / 100;
        return `${filled} ${circumference}`;
    }
    public clampPercent(value: number): number {
        if (Number.isNaN(value)) {
            return 0;
        }
        return Math.min(Math.max(value, 0), 120);
    }

    private startAnimation(targetSignal: { set: (v: number) => void; (): number }, target: number): void {
        this.stopAnimation(targetSignal);
        targetSignal.set(0);

        const startTime = performance.now();
        const duration = Math.max(target, 1) * 10; // 10 ms на 1% → 1.3s для 130%

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

        const blendHalfWidth = 5; // плавный переход ±5% вокруг порога

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
            normalized.length === 3
                ? normalized
                      .split('')
                      .map(ch => ch + ch)
                      .join('')
                : normalized;
        const num = parseInt(value, 16);
        const r = (num >> 16) & 0xff;
        const g = (num >> 8) & 0xff;
        const b = num & 0xff;
        return [r, g, b];
    }

    private readonly animationHandles = new Map<object, number>();

    private toHex(value: number): string {
        return value.toString(16).padStart(2, '0');
    }

    private parseColor(value: string): [number, number, number] {
        if (value.startsWith('#')) {
            return this.hexToChannels(value);
        }

        if (typeof document !== 'undefined') {
            const sample = document.createElement('span');
            sample.style.color = value;
            sample.style.display = 'none';
            document.body.appendChild(sample);
            const resolved = getComputedStyle(sample).color;
            document.body.removeChild(sample);
            const channels = resolved.match(/\d+/g)?.slice(0, 3).map(Number);
            if (channels?.length === 3) {
                return [channels[0], channels[1], channels[2]];
            }
        }

        return DashboardSummaryCardComponent.COLOR_FALLBACK_RGB;
    }

    private mixWithWhite(color: string, ratio: number): string {
        const [r, g, b] = this.parseColor(color);
        const mix = (c: number): number => Math.round(c + (255 - c) * ratio);
        return `#${this.toHex(mix(r))}${this.toHex(mix(g))}${this.toHex(mix(b))}`;
    }

    private mixWithDark(color: string, ratio: number): string {
        const [r, g, b] = this.parseColor(color);
        const mix = (c: number): number => Math.round(c * (1 - ratio));
        return `#${this.toHex(mix(r))}${this.toHex(mix(g))}${this.toHex(mix(b))}`;
    }

    private buildDefaultNutrientBars(): NutrientBar[] {
        return [
            {
                id: 'protein',
                label: 'Protein',
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                current: 110,
                target: 140,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-gradient-brand-start)',
                colorEnd: 'var(--fd-color-primary-600)',
            },
            {
                id: 'carbs',
                label: 'Carbs',
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                current: 180,
                target: 250,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-teal-500)',
                colorEnd: 'var(--fd-color-sky-500)',
            },
            {
                id: 'fats',
                label: 'Fats',
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                current: 45,
                target: 70,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-yellow-300)',
                colorEnd: 'var(--fd-color-orange-500)',
            },
            {
                id: 'fiber',
                label: 'Fiber',
                labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
                current: 18,
                target: 30,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-rose-500)',
                colorEnd: 'var(--fd-color-rose-500)',
            },
        ];
    }

    public getBarColor(bar: NutrientBar): string {
        if (!bar.target || bar.target <= 0) {
            return 'var(--fd-color-gray-500-static)';
        }
        const pct = (bar.current / bar.target) * 100;
        return this.getColorForPercent(pct);
    }
}
