import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiLineChartComponent, type FdUiLineChartPoint, type FdUiLineChartSeries } from 'fd-ui-kit';

import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import type { FastingCheckIn } from '../../../../shared/models/fasting.data';

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
        const readings = this.checkIns()
            .map(checkIn => ({ checkIn, timestamp: new Date(checkIn.checkedInAtUtc).getTime() }))
            .sort((left, right) => left.timestamp - right.timestamp);
        // The projection uses the endpoints only when there is a reading to plot.
        const [first] = readings;
        const [last] = readings.slice(-1);
        const plottedReadings = readings.map(({ checkIn, timestamp }) => {
            const timeRange = last.timestamp - first.timestamp;
            return {
                checkIn,
                label: this.formatAxisLabel(checkIn.checkedInAtUtc),
                xPosition: timeRange > 0 ? (timestamp - first.timestamp) / timeRange : undefined,
            };
        });
        const buildPoints = (getValue: (checkIn: FastingCheckIn) => number): FdUiLineChartPoint[] =>
            plottedReadings.map(({ checkIn, label, xPosition }) => ({ label, value: getValue(checkIn), xPosition }));

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
