import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Consumption } from '../../../types/consumption.data';
import {
    NutrientsSummaryComponent,
    NutrientsSummaryConfig,
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { NutrientChartData } from '../../../types/charts.data';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    FdUiConfirmDialogComponent,
    FdUiConfirmDialogData,
} from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';

@Component({
    selector: 'fd-consumption-detail',
    standalone: true,
    templateUrl: './consumption-detail.component.html',
    styleUrls: ['./consumption-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [DatePipe],
    imports: [
        CommonModule,
        TranslatePipe,
        DatePipe,
        DecimalPipe,
        NutrientsSummaryComponent,
        FdUiDialogComponent,
        FdUiButtonComponent,
    ],
})
export class ConsumptionDetailComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ConsumptionDetailComponent>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly datePipe = inject(DatePipe);
    private readonly translate = inject(TranslateService);

    public readonly consumption: Consumption;
    public readonly nutrientSummaryConfig: NutrientsSummaryConfig = {
        styles: {
            common: {
                infoBreakpoints: {
                    columnLayout: 680,
                },
            },
            charts: {
                chartBlockSize: 160,
                breakpoints: {
                    columnLayout: 680,
                },
            },
        },
    };

    public readonly calories: number;
    public readonly nutrientChartData: NutrientChartData;

    public constructor(@Inject(FD_UI_DIALOG_DATA) data: Consumption) {
        this.consumption = data;
        this.calories = this.consumption.totalCalories ?? 0;
        this.nutrientChartData = {
            proteins: this.consumption.totalProteins ?? 0,
            fats: this.consumption.totalFats ?? 0,
            carbs: this.consumption.totalCarbs ?? 0,
        };
    }

    public onEdit(): void {
        const editResult = new ConsumptionDetailActionResult(this.consumption.id, 'Edit');
        this.dialogRef.close(editResult);
    }

    public onDelete(): void {
        const formattedDate = this.datePipe.transform(this.consumption.date, 'dd.MM.yyyy');
        const data: FdUiConfirmDialogData = {
            title: this.translate.instant('CONSUMPTION_DETAIL.CONFIRM_DELETE_TITLE'),
            message: formattedDate ?? '',
            confirmLabel: this.translate.instant('CONSUMPTION_DETAIL.CONFIRM_BUTTON'),
            cancelLabel: this.translate.instant('CONSUMPTION_DETAIL.CANCEL_BUTTON'),
            danger: true,
        };

        this.fdDialogService
            .open(FdUiConfirmDialogComponent, {
                data,
                size: 'sm',
            })
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
        public id: number,
        public action: ConsumptionDetailAction,
    ) {}
}

export type ConsumptionDetailAction = 'Edit' | 'Delete';
