import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

import { ProductAddDialogComponent } from '../../../features/products/dialogs/product-add-dialog.component';
import { ProductListDialogComponent } from '../../../features/products/dialogs/product-list-dialog.component';
import type { Product } from '../../../features/products/models/product.data';
import { RecipeManageComponent } from '../../../features/recipes/components/manage/recipe-manage.component';
import { RecipeSelectDialogComponent } from '../../../features/recipes/dialogs/recipe-select-dialog.component';
import type { Recipe } from '../../../features/recipes/models/recipe.data';

export type ItemSelection = { type: 'Product'; product: Product } | { type: 'Recipe'; recipe: Recipe };

export type ItemSelectDialogData = {
    initialTab?: 'Product' | 'Recipe';
};

@Component({
    selector: 'fd-item-select-dialog',
    standalone: true,
    templateUrl: './item-select-dialog.component.html',
    styleUrls: ['./item-select-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FdUiTabsComponent,
        TranslatePipe,
        ProductListDialogComponent,
        RecipeSelectDialogComponent,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
    ],
})
export class ItemSelectDialogComponent {
    private readonly dialogData = inject<ItemSelectDialogData | null>(FD_UI_DIALOG_DATA, { optional: true });
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly dialogRef = inject(FdUiDialogRef<ItemSelectDialogComponent, ItemSelection | null>, { optional: true });

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
    public readonly activeTab = signal<'Product' | 'Recipe'>(this.dialogData?.initialTab === 'Recipe' ? 'Recipe' : 'Product');
    public readonly createActionLabelKey = computed(() =>
        this.activeTab() === 'Product' ? 'PRODUCT_LIST.ADD_PRODUCT_BUTTON' : 'RECIPE_SELECT_DIALOG.ADD_RECIPE_BUTTON',
    );

    public onProductSelected(product: Product): void {
        this.completeWith({ type: 'Product', product });
    }

    public onRecipeSelected(recipe: Recipe): void {
        this.completeWith({ type: 'Recipe', recipe });
    }

    public onCreateRecipeRequested(): void {
        this.completeWith(null);
    }

    public onCreateAction(): void {
        if (this.activeTab() === 'Product') {
            this.fdDialogService
                .open<ProductAddDialogComponent, Product | null, Product | null>(ProductAddDialogComponent, {
                    preset: 'fullscreen',
                })
                .afterClosed()
                .subscribe(product => {
                    if (!product) {
                        return;
                    }
                    this.completeWith({ type: 'Product', product });
                });
            return;
        }

        this.fdDialogService
            .open<RecipeManageComponent, null, Recipe | null>(RecipeManageComponent, {
                preset: 'fullscreen',
            })
            .afterClosed()
            .subscribe(recipe => {
                if (!recipe) {
                    return;
                }
                this.completeWith({ type: 'Recipe', recipe });
            });
    }

    private completeWith(selection: ItemSelection | null): void {
        if (!this.embedded() && this.dialogRef) {
            this.dialogRef.close(selection);
            return;
        }

        if (!selection) {
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
