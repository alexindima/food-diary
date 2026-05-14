import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { buildWearableMetrics } from '../../lib/wearable.mapper';
import type { WearableDailySummary } from '../../models/wearable.data';

@Component({
    selector: 'fd-wearable-daily-card',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './wearable-daily-card.component.html',
    styleUrls: ['./wearable-daily-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WearableDailyCardComponent {
    public readonly summary = input<WearableDailySummary | null>(null);
    public readonly metrics = computed(() => buildWearableMetrics(this.summary()));
}
