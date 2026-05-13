import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, switchMap, take } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { FavoriteProductService } from '../../../api/favorite-product.service';
import { ProductService } from '../../../api/product.service';
import type { Product } from '../../../models/product.data';
import { ProductDetailActionResult } from './product-detail.types';

@Injectable({ providedIn: 'root' })
export class ProductDetailFacade {
    private readonly productService = inject(ProductService);
    private readonly favoriteProductService = inject(FavoriteProductService);
    private readonly dialogRef = inject(FdUiDialogRef<unknown, ProductDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly isDuplicateInProgress = signal(false);

    private initialFavoriteState = false;
    private favoriteProductId: string | null = null;

    public initialize(product: Product): void {
        this.initialFavoriteState = product.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteProductId = product.favoriteProductId ?? null;

        this.favoriteProductService
            .isFavorite(product.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(isFav => {
                this.initialFavoriteState = isFav;
                this.isFavorite.set(isFav);
            });
    }

    public close(product: Product): void {
        if (this.hasFavoriteChanged()) {
            this.dialogRef.close(new ProductDetailActionResult(product.id, 'FavoriteChanged', true));
            return;
        }

        this.dialogRef.close();
    }

    public edit(product: Product): void {
        this.dialogRef.close(new ProductDetailActionResult(product.id, 'Edit', this.hasFavoriteChanged()));
    }

    public delete(product: Product): void {
        const data = this.buildConfirmDeleteData(product);

        this.fdDialogService
            .open(ConfirmDeleteDialogComponent, { data, size: 'sm' })
            .afterClosed()
            .pipe(take(1))
            .subscribe(confirm => {
                if (confirm === true) {
                    this.dialogRef.close(new ProductDetailActionResult(product.id, 'Delete', this.hasFavoriteChanged()));
                }
            });
    }

    public duplicate(product: Product): void {
        if (this.isDuplicateInProgress()) {
            return;
        }

        this.isDuplicateInProgress.set(true);
        this.productService
            .duplicate(product.id)
            .pipe(take(1))
            .subscribe({
                next: duplicated => {
                    this.dialogRef.close(new ProductDetailActionResult(duplicated.id, 'Duplicate', this.hasFavoriteChanged()));
                },
                error: () => {
                    this.isDuplicateInProgress.set(false);
                },
            });
    }

    public toggleFavorite(product: Product): void {
        if (this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            this.removeFavorite(product);
            return;
        }

        this.favoriteProductService
            .add(product.id)
            .pipe(take(1))
            .subscribe({
                next: favorite => {
                    this.isFavorite.set(true);
                    this.favoriteProductId = favorite.id;
                    this.isFavoriteLoading.set(false);
                },
                error: () => {
                    this.isFavoriteLoading.set(false);
                },
            });
    }

    public hasFavoriteChanged(): boolean {
        return this.initialFavoriteState !== this.isFavorite();
    }

    private removeFavorite(product: Product): void {
        const favoriteId = this.favoriteProductId;
        const request$ =
            favoriteId !== null && favoriteId.length > 0
                ? this.favoriteProductService.remove(favoriteId)
                : this.favoriteProductService.getAll().pipe(
                      switchMap(favorites => {
                          const match = favorites.find(favorite => favorite.productId === product.id);
                          return match === undefined ? of(null) : this.favoriteProductService.remove(match.id);
                      }),
                  );

        request$.pipe(take(1)).subscribe({
            next: () => {
                this.isFavorite.set(false);
                this.favoriteProductId = null;
                this.isFavoriteLoading.set(false);
            },
            error: () => {
                this.isFavoriteLoading.set(false);
            },
        });
    }

    private buildConfirmDeleteData(product: Product): ConfirmDeleteDialogData {
        return {
            title: this.translate.instant('CONFIRM_DELETE.TITLE', {
                type: this.translate.instant('PRODUCT_DETAIL.ENTITY_NAME'),
            }),
            message: this.translate.instant('CONFIRM_DELETE.MESSAGE', { name: product.name }),
            name: product.name,
            entityType: this.translate.instant('PRODUCT_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translate.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translate.instant('CONFIRM_DELETE.CANCEL'),
        };
    }
}
