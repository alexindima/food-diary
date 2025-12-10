import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Consumption } from '../../../types/consumption.data';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../../shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { CHART_COLORS } from '../../../constants/chart-colors';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';

@Component({
    selector: 'fd-consumption-detail',
    standalone: true,
    templateUrl: './consumption-detail.component.html',
    styleUrls: ['./consumption-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [DatePipe],
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        FdUiAccentSurfaceComponent,
        BaseChartDirective,
    ],
})
export class ConsumptionDetailComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ConsumptionDetailComponent>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly datePipe = inject(DatePipe);
    private readonly translate = inject(TranslateService);

    public readonly consumption: Consumption;
    public readonly calories: number;
    public readonly proteins: number;
    public readonly fats: number;
    public readonly carbs: number;
    public readonly fiber: number;
    public readonly itemsCount: number;
    public readonly formattedDate: string | null;
    public readonly mealTypeLabel: string | null;
    public readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'CONSUMPTION_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'CONSUMPTION_DETAIL.TABS.NUTRIENTS' },
    ];
    public activeTab: 'summary' | 'nutrients' = 'summary';
    public readonly pieChartData: ChartData<'pie', number[], string>;
    public readonly barChartData: ChartData<'bar', number[], string>;
    public readonly pieChartOptions: ChartOptions<'pie'>;
    public readonly barChartOptions: ChartOptions<'bar'>;
    public readonly chartSize = 200;
    public readonly macroBlocks: {
        labelKey: string;
        value: number;
        unitKey: string;
        color: string;
    }[];

    public constructor() {
        const data = inject<Consumption>(FD_UI_DIALOG_DATA);

        this.consumption = data;
        this.calories = data.totalCalories ?? 0;
        this.proteins = data.totalProteins ?? 0;
        this.fats = data.totalFats ?? 0;
        this.carbs = data.totalCarbs ?? 0;
        this.fiber = data.totalFiber ?? 0;
        this.itemsCount = data.items.length;
        this.formattedDate = this.datePipe.transform(this.consumption.date, 'dd.MM.yyyy, HH:mm');
        this.mealTypeLabel = data.mealType ? this.translate.instant(`MEAL_TYPES.${data.mealType}`) : null;

        const labels = [
            this.translate.instant('NUTRIENTS.PROTEINS'),
            this.translate.instant('NUTRIENTS.FATS'),
            this.translate.instant('NUTRIENTS.CARBS'),
        ];
        const datasetValues = [this.proteins, this.fats, this.carbs];
        const colors = [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs];
        this.pieChartData = {
            labels,
            datasets: [
                {
                    data: datasetValues,
                    backgroundColor: colors,
                },
            ],
        };
        this.barChartData = {
            labels,
            datasets: [
                {
                    data: datasetValues,
                    backgroundColor: colors,
                },
            ],
        };
        const tooltipLabel = (label: string, value: number) =>
            `${label}: ${value.toFixed(2)} ${this.translate.instant('STATISTICS.GRAMS')}`;
        this.pieChartOptions = {
            responsive: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
                    },
                },
            },
        };
        this.barChartOptions = {
            responsive: true,
            scales: {
                x: { display: false },
                y: { beginAtZero: true },
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
                    },
                },
            },
        };
        this.macroBlocks = [
            {
                labelKey: 'PRODUCT_LIST.PROTEINS',
                value: this.proteins,
                unitKey: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                color: CHART_COLORS.proteins,
            },
            {
                labelKey: 'PRODUCT_LIST.FATS',
                value: this.fats,
                unitKey: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                color: CHART_COLORS.fats,
            },
            {
                labelKey: 'PRODUCT_LIST.CARBS',
                value: this.carbs,
                unitKey: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                color: CHART_COLORS.carbs,
            },
            {
                labelKey: 'PRODUCT_DETAIL.SUMMARY.FIBER',
                value: this.fiber,
                unitKey: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                color: CHART_COLORS.fiber,
            },
        ];
    }

    public onTabChange(tab: string): void {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    }

    public onEdit(): void {
        const editResult = new ConsumptionDetailActionResult(this.consumption.id, 'Edit');
        this.dialogRef.close(editResult);
    }

    public onDelete(): void {
        const formattedDate = this.datePipe.transform(this.consumption.date, 'dd.MM.yyyy');
        const data: ConfirmDeleteDialogData = {
            title: this.translate.instant('CONFIRM_DELETE.TITLE', {
                type: this.translate.instant('CONSUMPTION_DETAIL.ENTITY_NAME'),
            }),
            message: this.translate.instant('CONFIRM_DELETE.MESSAGE', { name: formattedDate ?? '' }),
            name: formattedDate ?? '',
            entityType: this.translate.instant('CONSUMPTION_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translate.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translate.instant('CONFIRM_DELETE.CANCEL'),
        };

        this.fdDialogService
            .open(ConfirmDeleteDialogComponent, { data, size: 'sm' })
            .afterClosed()
            .subscribe(confirm => {
                if (confirm) {
                    const deleteResult = new ConsumptionDetailActionResult(this.consumption.id, 'Delete');
                    this.dialogRef.close(deleteResult);
                }
            });
    }
}

export class ConsumptionDetailActionResult {
    public constructor(
        public id: string,
        public action: ConsumptionDetailAction,
    ) {}
}

export type ConsumptionDetailAction = 'Edit' | 'Delete';
