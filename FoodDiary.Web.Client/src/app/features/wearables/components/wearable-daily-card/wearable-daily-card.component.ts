import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { WearableDailySummary } from '../../models/wearable.data';

type WearableMetric = {
    key: string;
    labelKey: string;
    icon: string;
    value: number;
    unit: string;
};

const MINUTES_PER_HOUR = 60;
const HOURS_PRECISION_FACTOR = 10;

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
        if (s === null) {
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
            result.push(
                this.createMetric(
                    'SLEEP',
                    '\uD83D\uDE34',
                    Math.round((s.sleepMinutes / MINUTES_PER_HOUR) * HOURS_PRECISION_FACTOR) / HOURS_PRECISION_FACTOR,
                    'h',
                ),
            );
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
