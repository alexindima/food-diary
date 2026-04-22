import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
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
    template: `
        <div class="card">
            <h3 class="card-title fd-ui-card-title">{{ 'WEARABLES.DAILY_TITLE' | translate }}</h3>

            @if (metrics().length === 0) {
                <p class="empty-state fd-ui-caption">{{ 'WEARABLES.NO_DATA' | translate }}</p>
            } @else {
                <div class="metrics-grid">
                    @for (metric of metrics(); track metric.key) {
                        <div class="metric-item">
                            <span class="metric-icon">{{ metric.icon }}</span>
                            <span class="metric-value fd-ui-metric-lg">{{ metric.value | number: '1.0-0' }}</span>
                            <span class="metric-unit fd-ui-helper-text">{{ metric.unit }}</span>
                            <span class="metric-label fd-ui-overline">{{ 'WEARABLES.' + metric.key | translate }}</span>
                        </div>
                    }
                </div>
            }
        </div>
    `,
    styles: [
        `
            .card {
                padding: 16px;
            }

            .card-title {
                margin: 0 0 16px;
            }

            .metrics-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(100px, 1fr));
                gap: 12px;
            }

            .metric-item {
                display: flex;
                flex-direction: column;
                align-items: center;
                padding: 12px 8px;
                border-radius: 12px;
                background: var(--fd-surface-variant, rgba(0, 0, 0, 0.02));
                gap: 2px;
            }

            .metric-icon {
                font-size: 24px;
            }

            .metric-value {
                --fd-text-metric-lg-size: 1.25rem;
                --fd-text-metric-lg-line-height: 1.1;
            }

            .metric-unit {
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
            }

            .metric-label {
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
            }

            .empty-state {
                text-align: center;
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
                padding: 16px;
            }
        `,
    ],
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
