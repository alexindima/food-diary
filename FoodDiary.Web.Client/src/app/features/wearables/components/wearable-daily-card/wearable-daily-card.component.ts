import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { WearableDailySummary } from '../../models/wearable.data';

interface WearableMetric {
    key: string;
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
            result.push({ key: 'STEPS', icon: '\uD83D\uDEB6', value: s.steps, unit: '' });
        }
        if (s.heartRate !== null) {
            result.push({ key: 'HEART_RATE', icon: '\u2764\uFE0F', value: s.heartRate, unit: 'bpm' });
        }
        if (s.caloriesBurned !== null) {
            result.push({ key: 'CALORIES_BURNED', icon: '\uD83D\uDD25', value: s.caloriesBurned, unit: 'kcal' });
        }
        if (s.activeMinutes !== null) {
            result.push({ key: 'ACTIVE_MINUTES', icon: '\u26A1', value: s.activeMinutes, unit: 'min' });
        }
        if (s.sleepMinutes !== null) {
            result.push({ key: 'SLEEP', icon: '\uD83D\uDE34', value: Math.round((s.sleepMinutes / 60) * 10) / 10, unit: 'h' });
        }
        return result;
    });
}
