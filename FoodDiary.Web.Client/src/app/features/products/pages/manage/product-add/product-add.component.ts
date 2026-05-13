import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { getRecordProperty, getStringProperty } from '../../../../../shared/lib/unknown-value.utils';
import type { OpenFoodFactsProduct } from '../../../api/open-food-facts.service';
import { ProductManageFormComponent } from '../../../components/manage/product-manage-form/product-manage-form.component';
import type { ProductManagePrefill } from '../../../components/manage/product-manage-lib/product-manage-form.types';

@Component({
    selector: 'fd-product-add',
    templateUrl: './product-add.component.html',
    styleUrls: ['./product-add.component.scss', '../../../components/manage/product-manage-form/product-manage-form.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ProductManageFormComponent],
})
export class ProductAddComponent {
    private readonly router = inject(Router);

    public readonly prefill = signal<ProductManagePrefill | null>(this.getNavigationPrefill());

    private getNavigationPrefill(): ProductManagePrefill | null {
        const state: unknown = this.router.currentNavigation()?.extras.state ?? (typeof history === 'undefined' ? null : history.state);
        const offProduct = getRecordProperty(state, 'offProduct');
        const barcode = getStringProperty(state, 'barcode');

        return {
            barcode: barcode ?? null,
            offProduct: this.isOpenFoodFactsProduct(offProduct) ? offProduct : null,
        };
    }

    private isOpenFoodFactsProduct(value: unknown): value is OpenFoodFactsProduct {
        return (
            typeof value === 'object' &&
            value !== null &&
            typeof Object.getOwnPropertyDescriptor(value, 'barcode')?.value === 'string' &&
            typeof Object.getOwnPropertyDescriptor(value, 'name')?.value === 'string'
        );
    }
}
