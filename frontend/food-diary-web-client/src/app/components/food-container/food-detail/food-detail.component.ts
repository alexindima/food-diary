import { ChangeDetectionStrategy, Component, inject, TemplateRef, ViewChild } from '@angular/core';
import { Food } from '../../../types/food.data';
import { TuiButton, TuiDialogContext, TuiDialogService } from '@taiga-ui/core';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'app-food-detail',
    templateUrl: './food-detail.component.html',
    styleUrls: ['./food-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, TuiButton]
})
export class FoodDetailComponent {
    public readonly context = injectContext<TuiDialogContext<FoodDetailActionResult, Food>>();

    public food: Food;

    public constructor() {
        this.food = this.context.data;
    }
    private readonly dialogService = inject(TuiDialogService);

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<boolean, void>>;

    public get isActionDisabled(): boolean {
        return this.food.usageCount > 0;
    }

    public onEdit(): void {
        const editResult = new FoodDetailActionResult(this.food.id, 'Edit');
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
                    const deleteResult = new FoodDetailActionResult(this.food.id, 'Delete');
                    this.context.completeWith(deleteResult);
                }
            });
    }
}

export class FoodDetailActionResult {
    public constructor(
        public id: number,
        public action: FoodDetailAction,
    ) {}
}

export type FoodDetailAction = 'Edit' | 'Delete';