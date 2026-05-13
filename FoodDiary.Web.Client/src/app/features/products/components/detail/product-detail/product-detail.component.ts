import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FormGroup } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { of, switchMap, take } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import {
    type NutritionControlNames,
    NutritionEditorComponent,
    type NutritionMacroState,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { normalizeQualityScore } from '../../../../../shared/lib/quality-score.utils';
import { FavoriteProductService } from '../../../api/favorite-product.service';
import { ProductService } from '../../../api/product.service';
import { buildProductTypeTranslationKey } from '../../../lib/product-type.utils';
import type { Product } from '../../../models/product.data';
import { ProductDetailActionResult, type ProductDetailTab } from '../product-detail-lib/product-detail.types';
import {
    buildProductDetailNutritionViewModel,
    type ProductDetailMacroBlock,
    type ProductDetailNutritionForm,
} from '../product-detail-lib/product-detail-nutrition.mapper';

@Component({
    selector: 'fd-product-detail',
    templateUrl: './product-detail.component.html',
    styleUrls: ['./product-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiDialogHeaderDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        FdUiAccentSurfaceComponent,
        NutritionEditorComponent,
    ],
})
export class ProductDetailComponent {
    private readonly productService = inject(ProductService);
    private readonly favoriteProductService = inject(FavoriteProductService);
    private readonly dialogRef = inject(FdUiDialogRef<ProductDetailComponent, ProductDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    private initialFavoriteState = false;
    private favoriteProductId: string | null = null;

    public product: Product;
    public readonly productTypeKey: string;
    public readonly baseUnitKey: string;
    public readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'PRODUCT_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'PRODUCT_DETAIL.TABS.NUTRIENTS' },
    ];
    public readonly activeTab = signal<ProductDetailTab>('summary');
    public readonly onTabChange = (tab: string): void => {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab.set(tab);
        }
    };

    public calories: number;
    public readonly qualityScore: number;
    public readonly qualityGrade: string;
    public readonly qualityHintKey: string;
    public readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    public readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'PRODUCT_DETAIL.REMOVE_FAVORITE' : 'PRODUCT_DETAIL.ADD_FAVORITE',
    );
    public readonly isDeleteDisabled = computed(() => !this.product.isOwnedByCurrentUser || this.product.usageCount > 0);
    public readonly isEditDisabled = computed(() => !this.product.isOwnedByCurrentUser || this.product.usageCount > 0);
    public readonly canModify = computed(() => !this.isEditDisabled());
    public readonly warningMessage = computed(() => {
        if (!this.isDeleteDisabled() && !this.isEditDisabled()) {
            return null;
        }

        return this.product.isOwnedByCurrentUser ? 'PRODUCT_DETAIL.WARNING_MESSAGE' : 'PRODUCT_DETAIL.WARNING_NOT_OWNER';
    });
    public readonly macroBlocks: ProductDetailMacroBlock[];
    public readonly macroSummaryBlocks: ProductDetailMacroBlock[];
    public readonly nutritionControlNames: NutritionControlNames = {
        calories: 'calories',
        proteins: 'proteins',
        fats: 'fats',
        carbs: 'carbs',
        fiber: 'fiber',
        alcohol: 'alcohol',
    };
    public readonly nutritionForm: FormGroup<ProductDetailNutritionForm>;
    public readonly macroBarState: NutritionMacroState;
    public readonly isDuplicateInProgress = signal(false);

    public constructor() {
        this.product = inject<Product>(FD_UI_DIALOG_DATA);
        this.initialFavoriteState = this.product.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteProductId = this.product.favoriteProductId ?? null;
        this.productTypeKey = buildProductTypeTranslationKey(this.product.productType ?? this.product.category ?? null);
        this.baseUnitKey = `GENERAL.UNITS.${this.product.baseUnit}`;
        this.qualityScore = normalizeQualityScore(this.product.qualityScore);
        this.qualityGrade = this.product.qualityGrade;
        this.qualityHintKey = `QUALITY.${this.qualityGrade.toUpperCase()}`;
        this.calories = this.product.caloriesPerBase;

        const nutritionViewModel = buildProductDetailNutritionViewModel(this.product);
        this.nutritionForm = nutritionViewModel.nutritionForm;
        this.macroBarState = nutritionViewModel.macroBarState;
        this.macroBlocks = nutritionViewModel.macroBlocks;
        this.macroSummaryBlocks = nutritionViewModel.macroSummaryBlocks;

        this.favoriteProductService
            .isFavorite(this.product.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(isFav => {
                this.initialFavoriteState = isFav;
                this.isFavorite.set(isFav);
            });
    }

    public close(): void {
        if (this.hasFavoriteChanged()) {
            this.dialogRef.close(new ProductDetailActionResult(this.product.id, 'FavoriteChanged', true));
            return;
        }

        this.dialogRef.close();
    }

    public onEdit(): void {
        if (this.isEditDisabled()) {
            return;
        }
        const editResult = new ProductDetailActionResult(this.product.id, 'Edit', this.hasFavoriteChanged());
        this.dialogRef.close(editResult);
    }

    public onDelete(): void {
        if (this.isDeleteDisabled()) {
            return;
        }
        const data: ConfirmDeleteDialogData = {
            title: this.translate.instant('CONFIRM_DELETE.TITLE', {
                type: this.translate.instant('PRODUCT_DETAIL.ENTITY_NAME'),
            }),
            message: this.translate.instant('CONFIRM_DELETE.MESSAGE', { name: this.product.name }),
            name: this.product.name,
            entityType: this.translate.instant('PRODUCT_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translate.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translate.instant('CONFIRM_DELETE.CANCEL'),
        };

        this.fdDialogService
            .open(ConfirmDeleteDialogComponent, { data, size: 'sm' })
            .afterClosed()
            .pipe(take(1))
            .subscribe(confirm => {
                if (confirm === true) {
                    const deleteResult = new ProductDetailActionResult(this.product.id, 'Delete', this.hasFavoriteChanged());
                    this.dialogRef.close(deleteResult);
                }
            });
    }

    public onDuplicate(): void {
        if (this.isDuplicateInProgress()) {
            return;
        }

        this.isDuplicateInProgress.set(true);
        this.productService
            .duplicate(this.product.id)
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

    public toggleFavorite(): void {
        if (this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            if (this.favoriteProductId !== null && this.favoriteProductId.length > 0) {
                this.favoriteProductService
                    .remove(this.favoriteProductId)
                    .pipe(take(1))
                    .subscribe({
                        next: () => {
                            this.isFavorite.set(false);
                            this.favoriteProductId = null;
                            this.isFavoriteLoading.set(false);
                        },
                        error: () => {
                            this.isFavoriteLoading.set(false);
                        },
                    });
                return;
            }

            this.favoriteProductService
                .getAll()
                .pipe(
                    switchMap(favorites => {
                        const match = favorites.find(f => f.productId === this.product.id);
                        return match === undefined ? of(null) : this.favoriteProductService.remove(match.id);
                    }),
                    take(1),
                )
                .subscribe({
                    next: () => {
                        this.isFavorite.set(false);
                        this.favoriteProductId = null;
                        this.isFavoriteLoading.set(false);
                    },
                    error: () => {
                        this.isFavoriteLoading.set(false);
                    },
                });
        } else {
            this.favoriteProductService
                .add(this.product.id)
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
    }

    private hasFavoriteChanged(): boolean {
        return this.initialFavoriteState !== this.isFavorite();
    }
}

export { ProductDetailActionResult } from '../product-detail-lib/product-detail.types';
