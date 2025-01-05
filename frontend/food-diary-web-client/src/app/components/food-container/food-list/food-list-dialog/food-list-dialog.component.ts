import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { FoodListBaseComponent } from '../food-list-base.component';
import { TuiButton, tuiDialog, TuiDialogContext, TuiLoader, TuiTextfieldComponent, TuiTextfieldDirective } from '@taiga-ui/core';
import { Food } from '../../../../types/food.data';
import { FoodConsumptionComponent } from '../../food-detail/food-consumption/food-consumption.component';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';

@Component({
    selector: 'app-food-list-dialog',
    templateUrl: '../food-list-base.component.html',
    styleUrls: ['./food-list-dialog.component.less', '../food-list-base.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiButton,
        TuiLoader,
        TuiPagination,
        TuiSearchComponent,
        TuiTextfieldComponent,
        TuiTextfieldControllerModule,
        TuiTextfieldDirective,
    ]
})
export class FoodListDialogComponent extends FoodListBaseComponent implements OnInit {
    public readonly context = injectContext<TuiDialogContext<Food, null>>();

    private readonly dialog = tuiDialog(FoodConsumptionComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    protected override async openFoodDetails(food: Food): Promise<void> {
        this.dialog(food).subscribe({
            next: data => {
                this.context.completeWith(data);
            },
        });
    }
}
