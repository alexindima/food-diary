import { ChangeDetectionStrategy, Component, inject, TemplateRef, ViewChild } from '@angular/core';
import { TuiButton, TuiDialogContext, TuiDialogService } from '@taiga-ui/core';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import { Consumption } from '../../../types/consumption.data';
import { DatePipe, DecimalPipe } from '@angular/common';

@Component({
    selector: 'app-consumption-detail',
    standalone: true,
    templateUrl: './consumption-detail.component.html',
    styleUrls: ['./consumption-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, DatePipe, TuiButton, DecimalPipe],
})
export class ConsumptionDetailComponent {
    public readonly context = injectContext<TuiDialogContext<ConsumptionDetailActionResult, Consumption>>();
    private readonly dialogService = inject(TuiDialogService);

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<boolean, void>>;
    public consumption: Consumption;

    public get totalCalories(): number {
        return this.consumption.items.reduce((sum, item) => sum + ((item.food?.caloriesPer100 ?? 0) * item.amount) / 100, 0);
    }

    public get totalProteins(): number {
        return this.consumption.items.reduce((sum, item) => sum + ((item.food?.proteinsPer100 ?? 0) * item.amount) / 100, 0);
    }

    public get totalFats(): number {
        return this.consumption.items.reduce((sum, item) => sum + ((item.food?.fatsPer100 ?? 0) * item.amount) / 100, 0);
    }

    public get totalCarbs(): number {
        return this.consumption.items.reduce((sum, item) => sum + ((item.food?.carbsPer100 ?? 0) * item.amount) / 100, 0);
    }

    public constructor() {
        this.consumption = this.context.data;
    }
    public onEdit(): void {
        const editResult = new ConsumptionDetailActionResult(this.consumption.id, 'Edit');
        this.context.completeWith(editResult);
    }

    public onDelete(): void {
        this.showConfirmDialog();
    }

    protected showConfirmDialog(): void {
        this.dialogService
            .open(this.confirmDialog, {
                dismissible: true,
                appearance: 'without-border-radius',
            })
            .subscribe(confirm => {
                if (confirm) {
                    const deleteResult = new ConsumptionDetailActionResult(this.consumption.id, 'Delete');
                    this.context.completeWith(deleteResult);
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
