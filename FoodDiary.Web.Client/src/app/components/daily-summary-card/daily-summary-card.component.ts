import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

export type DailySummaryMode = 'preview' | 'full';

export interface DailySummaryData {
    mode: DailySummaryMode;
    title?: string;
    eatenTodayKcal: number;
    goalKcal: number;
    percentage?: number;
    weeklyDiffText?: string;
    weeklyDiffType?: 'better' | 'worse' | 'neutral';
    lastMealTimeLabel?: string;
    lastMealTitle?: string;
    lastMealDescription?: string;
    lastMealUnitsLabel?: string;
    remainingKcal?: number;
    motivationText?: string;
    sectionTitle?: string;
    headerTitle?: string;
    showSettings?: boolean;
    onSettingsClick?: () => void;
    forecastText?: string;
    weeklyProgress?: WeeklyProgressPoint[];
}

export interface WeeklyProgressPoint {
    date: string | Date;
    calories: number;
    isToday?: boolean;
}

@Component({
    selector: 'app-daily-summary-card',
    standalone: true,
    imports: [CommonModule, FdUiAccentSurfaceComponent, FdUiButtonComponent, TranslatePipe, BaseChartDirective],
    templateUrl: './daily-summary-card.component.html',
    styleUrls: ['./daily-summary-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailySummaryCardComponent {
    @Input() public data!: DailySummaryData;

    private get rawProgress(): number {
        if (!this.data?.goalKcal) {
            return 0;
        }
        return (this.data.eatenTodayKcal / this.data.goalKcal) * 100;
    }

    public get progress(): number {
        return Math.min(100, Math.max(0, this.rawProgress));
    }

    public get motivationKey(): string | null {
        if (this.data?.motivationText) {
            return null;
        }

        const goal = this.data?.goalKcal ?? 0;
        if (goal <= 0) {
            return null;
        }

        const consumed = this.data?.eatenTodayKcal ?? 0;
        if (consumed <= 0) {
            return 'DAILY_PROGRESS_CARD.MOTIVATION.NONE';
        }

        const pct = (consumed / goal) * 100;

        if (pct <= 10) return 'DAILY_PROGRESS_CARD.MOTIVATION.P0_10';
        if (pct <= 20) return 'DAILY_PROGRESS_CARD.MOTIVATION.P10_20';
        if (pct <= 30) return 'DAILY_PROGRESS_CARD.MOTIVATION.P20_30';
        if (pct <= 40) return 'DAILY_PROGRESS_CARD.MOTIVATION.P30_40';
        if (pct <= 50) return 'DAILY_PROGRESS_CARD.MOTIVATION.P40_50';
        if (pct <= 60) return 'DAILY_PROGRESS_CARD.MOTIVATION.P50_60';
        if (pct <= 70) return 'DAILY_PROGRESS_CARD.MOTIVATION.P60_70';
        if (pct <= 80) return 'DAILY_PROGRESS_CARD.MOTIVATION.P70_80';
        if (pct <= 90) return 'DAILY_PROGRESS_CARD.MOTIVATION.P80_90';
        if (pct <= 110) return 'DAILY_PROGRESS_CARD.MOTIVATION.P90_110';
        if (pct <= 200) return 'DAILY_PROGRESS_CARD.MOTIVATION.P110_200';
        return 'DAILY_PROGRESS_CARD.MOTIVATION.ABOVE_200';
    }

    public get displayPercent(): number {
        if (this.data?.percentage !== undefined) {
            return this.data.percentage;
        }
        return Math.round(this.progress);
    }

    public get forecast(): string | null {
        if (this.data?.forecastText) {
            return this.data.forecastText;
        }

        const goal = this.data?.goalKcal ?? 0;
        if (goal <= 0) {
            return null;
        }

        const consumed = this.data?.eatenTodayKcal ?? 0;
        const pct = (consumed / goal) * 100;

        if (pct > 110) {
            return 'Если продолжите в таком темпе, вы превысите дневную цель.';
        }
        if (pct > 90) {
            return 'Темп близок к цели, следите за перекусами.';
        }
        return 'Сейчас вы укладываетесь в цель, продолжайте.';
    }

    public get weeklyBars(): { value: number; percent: number; displayPercent: number; isToday: boolean }[] {
        const points = this.data?.weeklyProgress ?? [];
        if (!points.length) {
            return [];
        }

        const normalized = points.map((p, index) => ({
            value: p.calories,
            isToday: p.isToday ?? index === points.length - 1,
        }));

        const maxValue = Math.max(...normalized.map(n => n.value), this.data?.goalKcal ?? 0, 1);

        return normalized.map(n => ({
            value: n.value,
            isToday: n.isToday,
            percent: Math.min(100, Math.round((n.value / maxValue) * 100)),
            displayPercent: Math.max(6, Math.min(100, Math.round((n.value / maxValue) * 100))),
        }));
    }

    public trackByIndex(index: number): number {
        return index;
    }

    public get weeklyLineChartData(): ChartConfiguration<'line'>['data'] | null {
        const bars = this.weeklyBars;
        if (!bars.length) {
            return null;
        }

        return {
            labels: bars.map(() => ''),
            datasets: [
                {
                    data: bars.map(b => b.value),
                    tension: 0.35,
                    borderColor: 'rgba(74, 108, 247, 0.85)',
                    backgroundColor: 'rgba(74, 108, 247, 0.12)',
                    fill: true,
                    pointRadius: bars.map(b => (b.isToday ? 5 : 3)),
                    pointHoverRadius: bars.map(b => (b.isToday ? 6 : 4)),
                    pointBackgroundColor: bars.map(b =>
                        b.isToday ? '#4a6cf7' : '#ffffff'
                    ),
                    pointBorderColor: bars.map(b =>
                        b.isToday ? '#4a6cf7' : 'rgba(74, 108, 247, 0.6)'
                    ),
                    pointBorderWidth: bars.map(b => (b.isToday ? 2 : 1)),
                },
            ],
        };
    }

    public readonly weeklyLineChartOptions: ChartConfiguration<'line'>['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: false,
            },
            tooltip: {
                enabled: false,
            },
        },
        scales: {
            x: {
                display: false,
                grid: { display: false },
            },
            y: {
                display: false,
                grid: { display: false },
                beginAtZero: true,
            },
        },
    };

    public get progressState(): 'blue' | 'green' | 'red' {
        const value = this.rawProgress;
        if (value < 90) {
            return 'blue';
        }
        if (value <= 110) {
            return 'green';
        }
        return 'red';
    }

    public get weeklyClass(): string {
        switch (this.data?.weeklyDiffType) {
            case 'better':
                return 'weekly--better';
            case 'worse':
                return 'weekly--worse';
            default:
                return 'weekly--neutral';
        }
    }

    public get title(): string {
        return this.data?.title || 'Панель Food Diary';
    }
}
