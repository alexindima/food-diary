import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { FoodListBaseComponent } from '../food-list-base.component';
import { TuiButton, TuiDialogContext, TuiLoader, TuiTextfieldComponent, TuiTextfieldDirective } from '@taiga-ui/core';
import { Food } from '../../../../types/food.data';
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

    protected override async onFoodClick(food: Food): Promise<void> {
        this.context.completeWith(food);
    }
}
