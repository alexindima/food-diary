import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { TuiDialogContext } from '@taiga-ui/core';
import { TuiTabs } from '@taiga-ui/kit';
import { TranslatePipe } from '@ngx-translate/core';
import { POLYMORPHEUS_CONTEXT } from '@taiga-ui/polymorpheus';
import { Product } from '../../../types/product.data';
import { Recipe } from '../../../types/recipe.data';
import { ProductListDialogComponent } from '../../product-container/product-list/product-list-dialog/product-list-dialog.component';
import { RecipeSelectDialogComponent } from '../../recipe-container/recipe-select-dialog/recipe-select-dialog.component';

export type ConsumptionItemSelection =
    | { type: 'Product'; product: Product }
    | { type: 'Recipe'; recipe: Recipe };

export type ConsumptionItemSelectDialogData = {
    initialTab?: 'Product' | 'Recipe';
};

@Component({
    selector: 'fd-consumption-item-select-dialog',
    standalone: true,
    templateUrl: './consumption-item-select-dialog.component.html',
    styleUrls: ['./consumption-item-select-dialog.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TuiTabs, TranslatePipe, ProductListDialogComponent, RecipeSelectDialogComponent],
})
export class ConsumptionItemSelectDialogComponent implements OnInit {
    public activeTabIndex = 0;

    private readonly context = inject<
        TuiDialogContext<ConsumptionItemSelection | null, ConsumptionItemSelectDialogData>
    >(POLYMORPHEUS_CONTEXT);

    public ngOnInit(): void {
        if (this.context.data?.initialTab === 'Recipe') {
            this.activeTabIndex = 1;
        }
    }

    public onTabChange(index: number): void {
        this.activeTabIndex = index;
    }

    public onProductSelected(product: Product): void {
        this.context.completeWith({ type: 'Product', product });
    }

    public onRecipeSelected(recipe: Recipe): void {
        this.context.completeWith({ type: 'Recipe', recipe });
    }

    public onCreateRecipeRequested(): void {
        this.context.completeWith(null);
    }
}
