import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectionStrategy, Component, NgZone, computed, inject, viewChild } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { TranslateService } from '@ngx-translate/core';
import { BaseChartDirective } from 'ng2-charts';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/material';
import { LocalizationService } from '../../../../services/localization.service';
import { FastingCheckIn } from '../../models/fasting.data';

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
export class FastingCheckInChartDialogComponent implements AfterViewInit {
    private readonly chartDirective = viewChild(BaseChartDirective);

    public readonly data = inject<FastingCheckInChartDialogData>(FD_UI_DIALOG_DATA);

    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);
    private readonly ngZone = inject(NgZone);

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
                borderColor: '#f97316',
                backgroundColor: 'rgba(249, 115, 22, 0.12)',
                tension: 0.3,
                borderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 5,
                fill: false,
                pointBackgroundColor: '#ffffff',
                pointBorderColor: '#f97316',
                pointBorderWidth: 2,
            },
            {
                label: this.translateService.instant('FASTING.CHECK_IN.ENERGY'),
                data: this.points().map(point => point.energyLevel),
                borderColor: '#2563eb',
                backgroundColor: 'rgba(37, 99, 235, 0.12)',
                tension: 0.3,
                borderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 5,
                fill: false,
                pointBackgroundColor: '#ffffff',
                pointBorderColor: '#2563eb',
                pointBorderWidth: 2,
            },
            {
                label: this.translateService.instant('FASTING.CHECK_IN.MOOD'),
                data: this.points().map(point => point.moodLevel),
                borderColor: '#7c3aed',
                backgroundColor: 'rgba(124, 58, 237, 0.12)',
                tension: 0.3,
                borderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 5,
                fill: false,
                pointBackgroundColor: '#ffffff',
                pointBorderColor: '#7c3aed',
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
                    color: '#334155',
                    font: {
                        size: 12,
                        weight: 600,
                    },
                },
            },
            tooltip: {
                displayColors: true,
                backgroundColor: '#0f172a',
                titleColor: '#e2e8f0',
                bodyColor: '#e2e8f0',
                footerColor: '#cbd5e1',
                padding: 10,
                callbacks: {
                    title: items => {
                        const point = this.points()[items[0]?.dataIndex ?? -1];
                        return point ? this.formatTooltipTitle(point.checkedInAtUtc) : '';
                    },
                    footer: items => {
                        const point = this.points()[items[0]?.dataIndex ?? -1];
                        if (!point) {
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

                        if (point.notes) {
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
                    color: 'rgba(226, 232, 240, 0.8)',
                },
                ticks: {
                    color: '#64748b',
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
                    color: '#64748b',
                },
                grid: {
                    color: 'rgba(226, 232, 240, 0.9)',
                },
            },
        },
    };

    public ngAfterViewInit(): void {
        this.ngZone.runOutsideAngular(() => {
            window.setTimeout(() => {
                this.chartDirective()?.chart?.resize();
                void this.chartDirective()?.update();
            }, 180);
        });
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
