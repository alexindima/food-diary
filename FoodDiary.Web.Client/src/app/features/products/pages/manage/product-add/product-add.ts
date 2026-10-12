import { ChangeDetectionStrategy, Component, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';

import type { UnsavedEditor } from '../../../../../shared/forms/unsaved-editor';
import { getRecordProperty, getStringProperty } from '../../../../../shared/lib/unknown-value.utils';
import { ProductManageFormComponent } from '../../../components/manage/product-manage-form/product-manage-form';
import type { ProductManagePrefill } from '../../../components/manage/product-manage-lib/product-manage-form.types';
import type { OpenFoodFactsProduct } from '../../../models/open-food-facts.data';

@Component({
    selector: 'fd-product-add',
    templateUrl: './product-add.html',
    styleUrls: ['./product-add.scss', '../../../components/manage/product-manage-form/product-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ProductManageFormComponent],
})
export class ProductAddComponent implements UnsavedEditor {
    private readonly editor = viewChild(ProductManageFormComponent);
    private readonly router = inject(Router);
    protected readonly prefill = signal<ProductManagePrefill | null>(this.getNavigationPrefill());

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        return this.editor()?.canLeaveAsync() ?? true;
    }
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
