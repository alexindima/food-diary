import { type HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { PremiumRequiredDialogComponent } from '../../../components/shared/premium-required-dialog/premium-required-dialog.component';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { ProductService } from '../api/product.service';
import { ProductSaveSuccessDialogComponent, type ProductSaveSuccessDialogData } from '../dialogs/product-save-success-dialog.component';
import { type CreateProductRequest, type Product, type UpdateProductRequest } from '../models/product.data';

export type RedirectAction = 'Home' | 'ProductList';

@Injectable({ providedIn: 'root' })
export class ProductManageFacade {
    private readonly productService = inject(ProductService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly authService = inject(AuthService);

    public async confirmDiscardChangesAsync(data: ConfirmDeleteDialogData): Promise<boolean> {
        const confirmed = await firstValueFrom(
            this.fdDialogService
                .open<ConfirmDeleteDialogComponent, ConfirmDeleteDialogData, boolean>(ConfirmDeleteDialogComponent, {
                    preset: 'confirm',
                    data,
                })
                .afterClosed(),
        );

        return !!confirmed;
    }

    public ensurePremiumAccess(): boolean {
        if (this.authService.isPremium()) {
            return true;
        }

        this.fdDialogService
            .open<PremiumRequiredDialogComponent, never, boolean>(PremiumRequiredDialogComponent, { preset: 'confirm' })
            .afterClosed()
            .subscribe(confirmed => {
                if (confirmed) {
                    void this.navigationService.navigateToPremiumAccessAsync();
                }
            });
        return false;
    }

    public async deleteProductAsync(product: Product, confirmData: ConfirmDeleteDialogData): Promise<'deleted' | 'cancelled' | 'error'> {
        const confirmed = await firstValueFrom(
            this.fdDialogService.open(ConfirmDeleteDialogComponent, { data: confirmData, preset: 'confirm' }).afterClosed(),
        );

        if (!confirmed) {
            return 'cancelled';
        }

        try {
            await firstValueFrom(this.productService.deleteById(product.id));
            await this.navigationService.navigateToProductListAsync();
            return 'deleted';
        } catch {
            return 'error';
        }
    }

    public async submitProductAsync(
        product: Product | null,
        productData: CreateProductRequest,
        skipConfirmDialog: boolean,
        afterSave?: (product: Product) => Promise<void>,
    ): Promise<{ product: Product | null; error: HttpErrorResponse | null }> {
        try {
            const savedProduct = product
                ? await firstValueFrom(this.productService.update(product.id, this.buildUpdateProductRequest(productData)))
                : await firstValueFrom(this.productService.create(productData));

            await afterSave?.(savedProduct);

            if (!skipConfirmDialog) {
                await this.showConfirmDialogAsync(Boolean(product));
            }

            return { product: savedProduct, error: null };
        } catch (error) {
            return { product: null, error: error as HttpErrorResponse };
        }
    }

    private buildUpdateProductRequest(productData: CreateProductRequest): UpdateProductRequest {
        return {
            ...productData,
            clearBarcode: productData.barcode === null,
            clearBrand: productData.brand === null,
            clearCategory: productData.category === null,
            clearDescription: productData.description === null,
            clearComment: productData.comment === null,
            clearImageUrl: productData.imageUrl === null,
            clearImageAssetId: productData.imageAssetId === null,
        };
    }

    private async showConfirmDialogAsync(isEdit: boolean): Promise<void> {
        const data: ProductSaveSuccessDialogData = {
            isEdit,
        };

        const redirectAction = await firstValueFrom(
            this.fdDialogService
                .open<ProductSaveSuccessDialogComponent, ProductSaveSuccessDialogData, RedirectAction>(ProductSaveSuccessDialogComponent, {
                    preset: 'confirm',
                    data,
                })
                .afterClosed(),
        );

        if (redirectAction === 'Home') {
            await this.navigationService.navigateToHomeAsync();
        } else if (redirectAction === 'ProductList') {
            await this.navigationService.navigateToProductListAsync();
        }
    }
}
