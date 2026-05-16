import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { FastingStats } from '../../../models/fasting.data';

@Component({
    selector: 'fd-fasting-stats-card',
    imports: [DecimalPipe, TranslatePipe, FdUiAccentSurfaceComponent, FdUiCardComponent],
    templateUrl: './fasting-stats-card.component.html',
    styleUrl: '../../fasting-page/fasting-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingStatsCardComponent {
    public readonly stats = input.required<FastingStats | null>();

    protected readonly hasPersonalSummary = computed(() => {
        const stats = this.stats();

        return (
            stats !== null &&
            (stats.completionRateLast30Days > 0 ||
                stats.checkInRateLast30Days > 0 ||
                stats.lastCheckInAtUtc !== null ||
                stats.topSymptom !== null)
        );
    });
    protected readonly topSymptomLabelKey = computed(() => {
        const symptom = this.stats()?.topSymptom ?? null;
        return symptom === null ? 'FASTING.PERSONAL_SUMMARY.NO_SYMPTOM' : `FASTING.CHECK_IN.SYMPTOMS.${symptom.toUpperCase()}`;
    });
}
