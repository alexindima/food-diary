import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';

import type { StatisticsDietStabilityData } from '../../models/statistics-dashboard-card.models';

const STABILITY_DEVIATION_TOLERANCE_PERCENT = 20;

@Component({
    selector: 'fd-statistics-diet-stability-card',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './statistics-diet-stability-card.html',
    styleUrl: './statistics-diet-stability-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsDietStabilityCardComponent {
    public readonly data = input.required<StatisticsDietStabilityData>();

    protected readonly hasHighDeviation = computed(() => {
        const deviation = this.data().averageDeviationPercent;
        return deviation !== null && deviation > STABILITY_DEVIATION_TOLERANCE_PERCENT;
    });
}
