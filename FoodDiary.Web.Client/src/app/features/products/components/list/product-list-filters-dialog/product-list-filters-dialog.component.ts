import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiChipSelectComponent, type FdUiChipSelectOption, FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';

import { ProductType } from '../../../models/product.data';
import type {
    ProductListFiltersDialogData,
    ProductListFiltersDialogResult,
    ProductListVisibilityFilter,
} from './product-list-filters-dialog.types';

@Component({
    selector: 'fd-product-list-filters-dialog',
    templateUrl: './product-list-filters-dialog.component.html',
    styleUrls: ['./product-list-filters-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiChipSelectComponent,
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

    public visibilityValue: ProductListVisibilityFilter = this.data.onlyMine ? 'mine' : 'all';
    public readonly selectedTypeValues = linkedSignal(() => [...this.data.productTypes]);

    public readonly productTypes: ProductType[] = (Object.values(ProductType) as ProductType[]).filter(
        type => type !== ProductType.Unknown,
    );
    private readonly selectableProductTypes = new Set<string>(this.productTypes);
    public readonly productTypeOptions = computed(() =>
        this.productTypes.map<FdUiChipSelectOption>(type => {
            const label = this.translate.instant(`PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.${type.toUpperCase()}`);
            return {
                value: type,
                label,
                ariaLabel: label,
                hint: label,
            };
        }),
    );

    public onVisibilityChange(value: string): void {
        this.visibilityValue = value === 'mine' ? 'mine' : 'all';
    }

    public onSelectedTypesChange(values: string[]): void {
        this.selectedTypeValues.set(values.filter((value): value is ProductType => this.selectableProductTypes.has(value)));
    }

    public onApply(): void {
        this.dialogRef.close({
            onlyMine: this.visibilityValue === 'mine',
            productTypes: this.selectedTypeValues(),
        });
    }

    public onCancel(): void {
        this.dialogRef.close(null);
    }
}
