import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { WearableDailySummary } from '../../models/wearable.data';

interface WearableMetric {
    key: string;
    labelKey: string;
    icon: string;
    value: number;
    unit: string;
}

@Component({
    selector: 'fd-wearable-daily-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './wearable-daily-card.component.html',
    styleUrls: ['./wearable-daily-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WearableDailyCardComponent {
    public readonly summary = input<WearableDailySummary | null>(null);

    public readonly metrics = computed<WearableMetric[]>(() => {
        const s = this.summary();
        if (!s) {
            return [];
        }

        const result: WearableMetric[] = [];
        if (s.steps !== null) {
            result.push(this.createMetric('STEPS', '\uD83D\uDEB6', s.steps, ''));
        }
        if (s.heartRate !== null) {
            result.push(this.createMetric('HEART_RATE', '\u2764\uFE0F', s.heartRate, 'bpm'));
        }
        if (s.caloriesBurned !== null) {
            result.push(this.createMetric('CALORIES_BURNED', '\uD83D\uDD25', s.caloriesBurned, 'kcal'));
        }
        if (s.activeMinutes !== null) {
            result.push(this.createMetric('ACTIVE_MINUTES', '\u26A1', s.activeMinutes, 'min'));
        }
        if (s.sleepMinutes !== null) {
            result.push(this.createMetric('SLEEP', '\uD83D\uDE34', Math.round((s.sleepMinutes / 60) * 10) / 10, 'h'));
        }
        return result;
    });

    private createMetric(key: string, icon: string, value: number, unit: string): WearableMetric {
        return {
            key,
            labelKey: `WEARABLES.${key}`,
            icon,
            value,
            unit,
        };
    }
}
