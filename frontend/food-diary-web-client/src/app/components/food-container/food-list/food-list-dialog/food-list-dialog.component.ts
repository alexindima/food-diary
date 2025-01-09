import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { FoodListBaseComponent } from '../food-list-base.component';
import {
    TuiButton,
    tuiDialog,
    TuiDialogContext,
    TuiIcon,
    TuiLoader,
    TuiTextfieldComponent,
    TuiTextfieldDirective
} from '@taiga-ui/core';
import { Food } from '../../../../types/food.data';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import {
    FoodAddDialogComponent
} from '../../food-manage/food-add-dialog/food-add-dialog.component';

@Component({
    selector: 'fd-food-list-dialog',
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
        TuiIcon,
    ]
})
export class FoodListDialogComponent extends FoodListBaseComponent {
    public readonly context = injectContext<TuiDialogContext<Food, null>>();

    private readonly addFoodDialog = tuiDialog(FoodAddDialogComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    public override async onAddFoodClick(): Promise<void> {
        this.addFoodDialog(null).subscribe({
            next: (food) => {
                this.context.completeWith(food);
            },
        });
    }

    protected override async onFoodClick(food: Food): Promise<void> {
        this.context.completeWith(food);
    }
}
