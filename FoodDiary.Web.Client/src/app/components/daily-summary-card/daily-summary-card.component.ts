import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

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
}

@Component({
    selector: 'app-daily-summary-card',
    standalone: true,
    imports: [CommonModule, FdUiAccentSurfaceComponent, FdUiButtonComponent],
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
