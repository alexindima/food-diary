import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-consumption-ring-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiCardComponent],
    templateUrl: './consumption-ring-card.component.html',
    styleUrl: './consumption-ring-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConsumptionRingCardComponent {
    public readonly dailyGoal = input<number>(0);
    public readonly dailyConsumed = input<number>(0);
    public readonly weeklyConsumed = input<number>(0);
    public readonly weeklyGoal = input<number | null>(null);
    public readonly isDailyHovered = signal(false);
    public readonly isWeeklyHovered = signal(false);

    private readonly outerRadius = 112;
    private readonly innerRadius = 88;
    public readonly dailyCircumference = 2 * Math.PI * this.outerRadius;
    public readonly weeklyCircumference = 2 * Math.PI * this.innerRadius;
    private readonly gradientIdDaily = `consumption-ring-daily-${Math.random().toString(36).slice(2, 9)}`;
    private readonly gradientIdWeekly = `consumption-ring-weekly-${Math.random().toString(36).slice(2, 9)}`;
    private readonly colorStops = [
        { percent: 0, color: '#5aa9fa' },
        { percent: 50, color: '#3f8df0' },
        { percent: 70, color: '#3f8df0' },
        { percent: 80, color: '#34d399' },
        { percent: 90, color: '#22c55e' },
        { percent: 100, color: '#16a34a' },
        { percent: 110, color: '#f59e0b' },
        { percent: 120, color: '#f97316' },
        { percent: 130, color: '#ef4444' },
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
    public readonly weeklyPercent = computed(() =>
        this.calculatePercent(this.safeWeeklyConsumed(), this.normalizedWeeklyGoal())
    );

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

    private startAnimation(targetSignal: { set: (v: number) => void; (): number }, target: number): void {
        this.stopAnimation(targetSignal);
        targetSignal.set(0);

        const startTime = performance.now();
        const duration = Math.max(target, 1) * 10; // 10 ms на 1% → 1.3s для 130%

        const step = () => {
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
        this.isDailyHovered.set(state);
    }

    public setWeeklyHover(state: boolean): void {
        this.isWeeklyHovered.set(state);
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

    private lerpColor(hexA: string, hexB: string, t: number): string {
        const [r1, g1, b1] = this.hexToRgb(hexA);
        const [r2, g2, b2] = this.hexToRgb(hexB);
        const lerp = (a: number, b: number) => Math.round(a + (b - a) * t);
        const r = lerp(r1, r2);
        const g = lerp(g1, g2);
        const b = lerp(b1, b2);
        return `#${this.toHex(r)}${this.toHex(g)}${this.toHex(b)}`;
    }

    private hexToRgb(hex: string): [number, number, number] {
        const normalized = hex.replace('#', '');
        const value = normalized.length === 3 ? normalized.split('').map(ch => ch + ch).join('') : normalized;
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
            return this.hexToRgb(value);
        }

        const match = value.match(/rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)/i);
        if (match) {
            return [Number(match[1]), Number(match[2]), Number(match[3])];
        }

        return this.hexToRgb(this.colorStops[0]?.color ?? '#000000');
    }

    private mixWithWhite(color: string, ratio: number): string {
        const [r, g, b] = this.parseColor(color);
        const mix = (c: number) => Math.round(c + (255 - c) * ratio);
        return `rgb(${mix(r)}, ${mix(g)}, ${mix(b)})`;
    }

    private mixWithDark(color: string, ratio: number): string {
        const [r, g, b] = this.parseColor(color);
        const mix = (c: number) => Math.round(c * (1 - ratio));
        return `rgb(${mix(r)}, ${mix(g)}, ${mix(b)})`;
    }

    public get dailyGradientId(): string {
        return this.gradientIdDaily;
    }

    public get weeklyGradientId(): string {
        return this.gradientIdWeekly;
    }
}
