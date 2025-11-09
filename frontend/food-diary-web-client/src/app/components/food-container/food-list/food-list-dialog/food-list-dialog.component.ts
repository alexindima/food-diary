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
import { Product } from '../../../../types/product.data';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import {
    FoodAddDialogComponent
} from '../../food-manage/food-add-dialog/food-add-dialog.component';
import { CardComponent } from '../../../shared/card/card.component';

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
        CardComponent,
    ]
})
export class FoodListDialogComponent extends FoodListBaseComponent {
    public readonly context = injectContext<TuiDialogContext<Product, null>>();

    private readonly addProductDialog = tuiDialog(FoodAddDialogComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    public override async onAddProductClick(): Promise<void> {
        this.addProductDialog(null).subscribe({
            next: (product) => {
                this.context.completeWith(product);
            },
        });
    }

    protected override async onProductClick(product: Product): Promise<void> {
        this.context.completeWith(product);
    }
}
