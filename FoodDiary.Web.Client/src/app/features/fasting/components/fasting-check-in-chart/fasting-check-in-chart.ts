import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiLineChartComponent, type FdUiLineChartPoint, type FdUiLineChartSeries } from 'fd-ui-kit';

import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import type { FastingCheckIn } from '../../models/fasting.data';

const FASTING_CHECK_IN_MIN_LEVEL = 1;
const FASTING_CHECK_IN_MAX_LEVEL = 5;

@Component({
    selector: 'fd-fasting-check-in-chart',
    imports: [TranslatePipe, FdUiLineChartComponent],
    templateUrl: './fasting-check-in-chart.html',
    styleUrl: './fasting-check-in-chart.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingCheckInChartComponent {
    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);

    public readonly checkIns = input.required<readonly FastingCheckIn[]>();

    protected readonly minLevel = FASTING_CHECK_IN_MIN_LEVEL;
    protected readonly maxLevel = FASTING_CHECK_IN_MAX_LEVEL;
    protected readonly series = computed<readonly FdUiLineChartSeries[]>(() => {
        const checkIns = [...this.checkIns()].sort(
            (left, right) => new Date(left.checkedInAtUtc).getTime() - new Date(right.checkedInAtUtc).getTime(),
        );
        const timestamps = checkIns.map(checkIn => new Date(checkIn.checkedInAtUtc).getTime());
        const firstTimestamp = timestamps[0] ?? 0;
        const timeRange = (timestamps.at(-1) ?? firstTimestamp) - firstTimestamp;
        const buildPoints = (getValue: (checkIn: FastingCheckIn) => number): FdUiLineChartPoint[] =>
            checkIns.map((checkIn, index) => ({
                label: this.formatAxisLabel(checkIn.checkedInAtUtc),
                value: getValue(checkIn),
                xPosition: timeRange > 0 ? ((timestamps[index] ?? firstTimestamp) - firstTimestamp) / timeRange : undefined,
            }));

        return [
            {
                label: this.translateService.instant('FASTING.CHECK_IN.HUNGER'),
                color: 'var(--fd-color-orange-500)',
                points: buildPoints(checkIn => checkIn.hungerLevel),
            },
            {
                label: this.translateService.instant('FASTING.CHECK_IN.ENERGY'),
                color: 'var(--fd-color-primary-600)',
                points: buildPoints(checkIn => checkIn.energyLevel),
            },
            {
                label: this.translateService.instant('FASTING.CHECK_IN.MOOD'),
                color: 'var(--fd-color-purple-500)',
                points: buildPoints(checkIn => checkIn.moodLevel),
            },
        ];
    });

    private formatAxisLabel(value: string): string {
        return new Intl.DateTimeFormat(resolveAppLocale(this.localizationService.getCurrentLanguage()), {
            hour: '2-digit',
            minute: '2-digit',
        }).format(new Date(value));
    }
}
