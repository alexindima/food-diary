import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiSegmentedToggleComponent, FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import { ProductType } from '../../../../types/product.data';

export interface ProductListFiltersDialogData {
    onlyMine: boolean;
    productTypes: ProductType[];
}

export interface ProductListFiltersDialogResult {
    onlyMine: boolean;
    productTypes: ProductType[];
}

@Component({
    selector: 'fd-product-list-filters-dialog',
    templateUrl: './product-list-filters-dialog.component.html',
    styleUrls: ['./product-list-filters-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiSegmentedToggleComponent,
    ],
})
export class ProductListFiltersDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ProductListFiltersDialogComponent, ProductListFiltersDialogResult | null>);
    private readonly translate = inject(TranslateService);
    private readonly data = inject<ProductListFiltersDialogData>(FD_UI_DIALOG_DATA);

    public readonly visibilityOptions: FdUiSegmentedToggleOption[] = [
        { value: 'all', label: this.translate.instant('PRODUCT_LIST.FILTER_ALL_PRODUCTS') },
        { value: 'mine', label: this.translate.instant('PRODUCT_LIST.FILTER_MY_PRODUCTS') },
    ];

    public visibilityValue: 'all' | 'mine' = this.data.onlyMine ? 'mine' : 'all';
    public selectedTypes = new Set<ProductType>(this.data.productTypes ?? []);

    public readonly productTypes: ProductType[] = (Object.values(ProductType) as ProductType[])
        .filter(type => type !== ProductType.Unknown);

    public onVisibilityChange(value: string): void {
        this.visibilityValue = value === 'mine' ? 'mine' : 'all';
    }

    public isTypeSelected(type: ProductType): boolean {
        return this.selectedTypes.has(type);
    }

    public toggleType(type: ProductType): void {
        if (this.selectedTypes.has(type)) {
            this.selectedTypes.delete(type);
            return;
        }

        this.selectedTypes.add(type);
    }

    public onApply(): void {
        this.dialogRef.close({
            onlyMine: this.visibilityValue === 'mine',
            productTypes: this.productTypes.filter(type => this.selectedTypes.has(type)),
        });
    }

    public onCancel(): void {
        this.dialogRef.close(null);
    }
}
