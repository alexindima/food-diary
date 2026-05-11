import { CommonModule } from '@angular/common';
import { afterNextRender, ChangeDetectionStrategy, Component, computed, effect, inject, NgZone, viewChild } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { BaseChartDirective } from 'ng2-charts';

import { LocalizationService } from '../../../../services/localization.service';
import type { FastingCheckIn } from '../../models/fasting.data';

const INITIAL_CHART_RESIZE_DELAY_MS = 180;

export interface FastingCheckInChartDialogData {
    title: string;
    subtitle: string;
    checkIns: FastingCheckIn[];
}

interface FastingCheckInChartPoint {
    checkedInAtUtc: string;
    hungerLevel: number;
    energyLevel: number;
    moodLevel: number;
    symptoms: string[];
    notes: string | null;
}

@Component({
    selector: 'fd-fasting-checkin-chart-dialog',
    standalone: true,
    imports: [CommonModule, BaseChartDirective, FdUiDialogShellComponent],
    templateUrl: './fasting-checkin-chart-dialog.component.html',
    styleUrl: './fasting-checkin-chart-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [provideCharts(withDefaultRegisterables())],
})
export class FastingCheckInChartDialogComponent {
    private readonly chartDirective = viewChild(BaseChartDirective);

    public readonly data = inject<FastingCheckInChartDialogData>(FD_UI_DIALOG_DATA);

    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);
    private readonly ngZone = inject(NgZone);
    private hasScheduledInitialChartResize = false;

    public readonly points = computed<FastingCheckInChartPoint[]>(() =>
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

    public readonly chartData = computed<ChartConfiguration<'line'>['data']>(() => ({
        labels: this.points().map(point => this.formatAxisLabel(point.checkedInAtUtc)),
        datasets: [
            {
                label: this.translateService.instant('FASTING.CHECK_IN.HUNGER'),
                data: this.points().map(point => point.hungerLevel),
                borderColor: 'var(--fd-color-orange-500)',
                backgroundColor: 'color-mix(in srgb, var(--fd-color-orange-500) 12%, transparent)',
                tension: 0.3,
                borderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 5,
                fill: false,
                pointBackgroundColor: 'var(--fd-color-white)',
                pointBorderColor: 'var(--fd-color-orange-500)',
                pointBorderWidth: 2,
            },
            {
                label: this.translateService.instant('FASTING.CHECK_IN.ENERGY'),
                data: this.points().map(point => point.energyLevel),
                borderColor: 'var(--fd-color-primary-600)',
                backgroundColor: 'color-mix(in srgb, var(--fd-color-primary-600) 12%, transparent)',
                tension: 0.3,
                borderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 5,
                fill: false,
                pointBackgroundColor: 'var(--fd-color-white)',
                pointBorderColor: 'var(--fd-color-primary-600)',
                pointBorderWidth: 2,
            },
            {
                label: this.translateService.instant('FASTING.CHECK_IN.MOOD'),
                data: this.points().map(point => point.moodLevel),
                borderColor: 'var(--fd-color-purple-500)',
                backgroundColor: 'color-mix(in srgb, var(--fd-color-purple-500) 12%, transparent)',
                tension: 0.3,
                borderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 5,
                fill: false,
                pointBackgroundColor: 'var(--fd-color-white)',
                pointBorderColor: 'var(--fd-color-purple-500)',
                pointBorderWidth: 2,
            },
        ],
    }));

    public readonly chartOptions: ChartConfiguration<'line'>['options'] = {
        responsive: true,
        maintainAspectRatio: true,
        aspectRatio: 3.2,
        animation: false,
        plugins: {
            legend: {
                display: true,
                position: 'bottom',
                labels: {
                    usePointStyle: true,
                    boxWidth: 10,
                    color: 'var(--fd-color-slate-700)',
                    font: {
                        size: 12,
                        weight: 600,
                    },
                },
            },
            tooltip: {
                displayColors: true,
                backgroundColor: 'var(--fd-color-slate-900)',
                titleColor: 'var(--fd-color-slate-200)',
                bodyColor: 'var(--fd-color-slate-200)',
                footerColor: 'var(--fd-color-slate-300)',
                padding: 10,
                callbacks: {
                    title: items => {
                        const point = this.getTooltipPoint(items);
                        return point === undefined ? '' : this.formatTooltipTitle(point.checkedInAtUtc);
                    },
                    footer: items => {
                        const point = this.getTooltipPoint(items);
                        if (point === undefined) {
                            return '';
                        }

                        const parts: string[] = [];
                        if (point.symptoms.length > 0) {
                            parts.push(
                                this.translateService.instant('FASTING.CHECK_IN.CHART_TOOLTIP_SYMPTOMS', {
                                    value: point.symptoms.map(symptom => this.getSymptomLabel(symptom)).join(', '),
                                }),
                            );
                        }

                        if (point.notes !== null && point.notes.length > 0) {
                            parts.push(
                                this.translateService.instant('FASTING.CHECK_IN.CHART_TOOLTIP_NOTES', {
                                    value: point.notes,
                                }),
                            );
                        }

                        return parts;
                    },
                },
            },
        },
        scales: {
            x: {
                grid: {
                    color: 'color-mix(in srgb, var(--fd-color-slate-200) 80%, transparent)',
                },
                ticks: {
                    color: 'var(--fd-color-slate-500)',
                    maxRotation: 0,
                    autoSkip: true,
                    maxTicksLimit: 8,
                    autoSkipPadding: 24,
                },
            },
            y: {
                min: 1,
                max: 5,
                ticks: {
                    stepSize: 1,
                    color: 'var(--fd-color-slate-500)',
                },
                grid: {
                    color: 'color-mix(in srgb, var(--fd-color-slate-200) 90%, transparent)',
                },
            },
        },
    };

    public constructor() {
        effect(() => {
            const chartDirective = this.chartDirective();
            if (chartDirective === undefined || this.hasScheduledInitialChartResize) {
                return;
            }

            this.hasScheduledInitialChartResize = true;
            afterNextRender(() => {
                this.ngZone.runOutsideAngular(() => {
                    window.setTimeout(() => {
                        chartDirective.chart?.resize();
                        chartDirective.update();
                    }, INITIAL_CHART_RESIZE_DELAY_MS);
                });
            });
        });
    }

    private getTooltipPoint(items: readonly { dataIndex: number }[]): FastingCheckInChartPoint | undefined {
        const tooltipItem = (items as readonly ({ dataIndex: number } | undefined)[])[0];
        if (tooltipItem === undefined) {
            return undefined;
        }

        return (this.points() as readonly (FastingCheckInChartPoint | undefined)[])[tooltipItem.dataIndex];
    }

    private formatAxisLabel(value: string): string {
        return new Intl.DateTimeFormat(this.getLocale(), {
            hour: '2-digit',
            minute: '2-digit',
        }).format(new Date(value));
    }

    private formatTooltipTitle(value: string): string {
        return new Intl.DateTimeFormat(this.getLocale(), {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        }).format(new Date(value));
    }

    private getLocale(): string {
        return this.localizationService.getCurrentLanguage() === 'ru' ? 'ru-RU' : 'en-US';
    }

    private getSymptomLabel(symptom: string): string {
        return this.translateService.instant(`FASTING.CHECK_IN.SYMPTOMS.${symptom.toUpperCase()}`);
    }
}
