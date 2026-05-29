import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiLineChartComponent, type FdUiLineChartPoint } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';

import { LocalizationService } from '../../../../services/localization.service';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import type { FastingCheckIn } from '../../models/fasting.data';

const FASTING_CHECK_IN_MIN_LEVEL = 1;
const FASTING_CHECK_IN_MAX_LEVEL = 5;

export type FastingCheckInChartDialogData = {
    title: string;
    subtitle: string;
    checkIns: FastingCheckIn[];
};

type FastingCheckInChartPoint = {
    checkedInAtUtc: string;
    hungerLevel: number;
    energyLevel: number;
    moodLevel: number;
    symptoms: string[];
    notes: string | null;
};

type FastingCheckInChartSeries = {
    key: string;
    label: string;
    color: string;
    points: readonly FdUiLineChartPoint[];
};

@Component({
    selector: 'fd-fasting-checkin-chart-dialog',
    imports: [CommonModule, FdUiDialogShellComponent, FdUiLineChartComponent],
    templateUrl: './fasting-checkin-chart-dialog.component.html',
    styleUrl: './fasting-checkin-chart-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingCheckInChartDialogComponent {
    protected readonly data = inject<FastingCheckInChartDialogData>(FD_UI_DIALOG_DATA);

    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);

    protected readonly minLevel = FASTING_CHECK_IN_MIN_LEVEL;
    protected readonly maxLevel = FASTING_CHECK_IN_MAX_LEVEL;

    protected readonly points = computed<FastingCheckInChartPoint[]>(() =>
        [...this.data.checkIns]
            .sort((left, right) => new Date(left.checkedInAtUtc).getTime() - new Date(right.checkedInAtUtc).getTime())
            .map(checkIn => ({
                checkedInAtUtc: checkIn.checkedInAtUtc,
                hungerLevel: checkIn.hungerLevel,
                energyLevel: checkIn.energyLevel,
                moodLevel: checkIn.moodLevel,
                symptoms: checkIn.symptoms,
                notes: checkIn.notes,
            })),
    );

    protected readonly chartSeries = computed<FastingCheckInChartSeries[]>(() => {
        const points = this.points();
        const buildPoints = (getValue: (point: FastingCheckInChartPoint) => number): FdUiLineChartPoint[] =>
            points.map(point => ({
                label: this.formatAxisLabel(point.checkedInAtUtc),
                value: getValue(point),
            }));

        return [
            {
                key: 'hunger',
                label: this.translateService.instant('FASTING.CHECK_IN.HUNGER'),
                color: 'var(--fd-color-orange-500)',
                points: buildPoints(point => point.hungerLevel),
            },
            {
                key: 'energy',
                label: this.translateService.instant('FASTING.CHECK_IN.ENERGY'),
                color: 'var(--fd-color-primary-600)',
                points: buildPoints(point => point.energyLevel),
            },
            {
                key: 'mood',
                label: this.translateService.instant('FASTING.CHECK_IN.MOOD'),
                color: 'var(--fd-color-purple-500)',
                points: buildPoints(point => point.moodLevel),
            },
        ];
    });

    private formatAxisLabel(value: string): string {
        return new Intl.DateTimeFormat(this.getLocale(), {
            hour: '2-digit',
            minute: '2-digit',
        }).format(new Date(value));
    }

    private getLocale(): string {
        return resolveAppLocale(this.localizationService.getCurrentLanguage());
    }
}
