import { HttpErrorResponse } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { PremiumRequiredDialogComponent } from '../../../components/shared/premium-required-dialog/premium-required-dialog';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { getNumberProperty, getRecordProperty } from '../../../shared/lib/unknown-value.utils';
import { ProductService } from '../api/product.service';
import type { CreateProductRequest, Product, UpdateProductRequest } from '../models/product.data';
import type { ProductDeleteResult } from './product-manage.types';

@Service()
export class ProductManageFacade {
    private readonly productService = inject(ProductService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly authService = inject(AuthService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);

    public async confirmDiscardChangesAsync(data: ConfirmDeleteDialogData): Promise<boolean> {
        const confirmed = await firstValueFrom(
            this.fdDialogService
                .open<ConfirmDeleteDialogComponent, ConfirmDeleteDialogData, boolean>(ConfirmDeleteDialogComponent, {
                    preset: 'confirm',
                    data,
                })
                .afterClosed(),
        );

        return confirmed === true;
    }

    public ensurePremiumAccess(): boolean {
        if (this.authService.isPremium()) {
            return true;
        }

        this.fdDialogService
            .open<PremiumRequiredDialogComponent, never, boolean>(PremiumRequiredDialogComponent, { preset: 'confirm' })
            .afterClosed()
            .subscribe(confirmed => {
                if (confirmed === true) {
                    void this.navigationService.navigateToPremiumAccessAsync();
                }
            });
        return false;
    }

    public async deleteProductAsync(product: Product, confirmData: ConfirmDeleteDialogData): Promise<ProductDeleteResult> {
        const confirmed = await firstValueFrom(
            this.fdDialogService.open(ConfirmDeleteDialogComponent, { data: confirmData, preset: 'confirm' }).afterClosed(),
        );

        if (confirmed !== true) {
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
        skipPostSaveRedirect: boolean,
        afterSave?: (product: Product) => Promise<void>,
    ): Promise<{ product: Product | null; error: HttpErrorResponse | null }> {
        try {
            const isEdit = product !== null;
            const savedProduct = isEdit
                ? await firstValueFrom(this.productService.update(product.id, this.buildUpdateProductRequest(productData)))
                : await firstValueFrom(this.productService.create(productData));

            let afterSaveError: HttpErrorResponse | null = null;
            try {
                await afterSave?.(savedProduct);
            } catch (error) {
                afterSaveError = this.toHttpErrorResponse(error);
            }

            if (!skipPostSaveRedirect && afterSaveError === null) {
                this.toastService.success(
                    this.translateService.instant(isEdit ? 'PRODUCT_DETAIL.EDIT_SUCCESS' : 'PRODUCT_DETAIL.CREATE_SUCCESS'),
                );
                await this.navigationService.navigateToProductListAsync();
            }

            return { product: savedProduct, error: afterSaveError };
        } catch (error) {
            return { product: null, error: this.toHttpErrorResponse(error) };
        }
    }

    private toHttpErrorResponse(error: unknown): HttpErrorResponse {
        if (error instanceof HttpErrorResponse) {
            return error;
        }

        return new HttpErrorResponse({
            error: getRecordProperty(error, 'error') ?? error,
            status: getNumberProperty(error, 'status') ?? 0,
        });
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
}
