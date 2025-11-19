import { ChangeDetectionStrategy, Component, OnInit, inject, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Product } from '../../../types/product.data';
import { Recipe } from '../../../types/recipe.data';
import { ProductListDialogComponent } from '../../product-container/product-list/product-list-dialog/product-list-dialog.component';
import { RecipeSelectDialogComponent } from '../../recipe-container/recipe-select-dialog/recipe-select-dialog.component';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';

import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

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
    styleUrls: ['./consumption-item-select-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiTabsComponent, TranslatePipe, ProductListDialogComponent, RecipeSelectDialogComponent],
})
export class ConsumptionItemSelectDialogComponent implements OnInit {
    private readonly dialogData = inject<ConsumptionItemSelectDialogData | null>(FD_UI_DIALOG_DATA, { optional: true });

    public readonly embedded = input<boolean>(false);
    public readonly productSelected = output<Product>();
    public readonly recipeSelected = output<Recipe>();
    public readonly createRecipeRequested = output<void>();
    public readonly tabs: FdUiTab[] = [
        {
            value: 'Product',
            labelKey: 'CONSUMPTION_MANAGE.ITEM_SELECT_DIALOG.PRODUCTS_TAB',
        },
        {
            value: 'Recipe',
            labelKey: 'CONSUMPTION_MANAGE.ITEM_SELECT_DIALOG.RECIPES_TAB',
        },
    ];
    public activeTab: 'Product' | 'Recipe' = 'Product';
    private readonly dialogRef = inject(
        FdUiDialogRef<ConsumptionItemSelectDialogComponent, ConsumptionItemSelection | null>,
        { optional: true },
    );

    public ngOnInit(): void {
        if (!this.embedded() && this.dialogData?.initialTab === 'Recipe') {
            this.activeTab = 'Recipe';
        }
    }

    public onProductSelected(product: Product): void {
        this.completeWith({ type: 'Product', product });
    }

    public onRecipeSelected(recipe: Recipe): void {
        this.completeWith({ type: 'Recipe', recipe });
    }

    public onCreateRecipeRequested(): void {
        this.completeWith(null);
    }

    private completeWith(selection: ConsumptionItemSelection | null): void {
        if (!this.embedded() && this.dialogRef) {
            this.dialogRef.close(selection);
            return;
        }

        if (!selection) {
            // TODO: The 'emit' function requires a mandatory void argument
            this.createRecipeRequested.emit();
            return;
        }

        if (selection.type === 'Product') {
            this.productSelected.emit(selection.product);
        } else {
            this.recipeSelected.emit(selection.recipe);
        }
    }
}
