import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { TuiTabs } from '@taiga-ui/kit';
import { TranslatePipe } from '@ngx-translate/core';
import { Product } from '../../../types/product.data';
import { Recipe } from '../../../types/recipe.data';
import { ProductListDialogComponent } from '../../product-container/product-list/product-list-dialog/product-list-dialog.component';
import { RecipeSelectDialogComponent } from '../../recipe-container/recipe-select-dialog/recipe-select-dialog.component';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Inject, Optional } from '@angular/core';

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
    @Input() public embedded: boolean = false;
    @Output() public productSelected = new EventEmitter<Product>();
    @Output() public recipeSelected = new EventEmitter<Recipe>();
    @Output() public createRecipeRequested = new EventEmitter<void>();
    public activeTabIndex = 0;
    private readonly dialogRef = inject(
        MatDialogRef<ConsumptionItemSelectDialogComponent, ConsumptionItemSelection | null>,
        { optional: true },
    );

    public constructor(
        @Optional()
        @Inject(MAT_DIALOG_DATA)
        private readonly dialogData: ConsumptionItemSelectDialogData | null,
    ) {}

    public ngOnInit(): void {
        if (!this.embedded && this.dialogData?.initialTab === 'Recipe') {
            this.activeTabIndex = 1;
        }
    }

    public onTabChange(index: number): void {
        this.activeTabIndex = index;
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
        if (!this.embedded && this.dialogRef) {
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
