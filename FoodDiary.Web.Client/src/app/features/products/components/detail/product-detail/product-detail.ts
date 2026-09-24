import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { type FieldTree, form } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';

import type { NutritionFormModel, NutritionMacroState } from '../../../../../components/shared/nutrition-editor/nutrition-editor';
import { normalizeQualityScore } from '../../../../../shared/lib/quality-score.utils';
import { ChartColorsService } from '../../../../../shared/theme/chart-colors.service';
import { QuickMealService } from '../../../../meals/lib/quick/quick-meal.service';
import { ProductDetailFacade } from '../../../lib/detail/product-detail.facade';
import { buildProductTypeTranslationKey } from '../../../lib/product-type.utils';
import type { Product } from '../../../models/product.data';
import { ProductDetailActionsComponent } from '../product-detail-actions/product-detail-actions';
import { buildProductDetailNutritionViewModel, type ProductDetailMacroBlock } from '../product-detail-lib/product-detail-nutrition.mapper';
import { ProductDetailSummaryComponent } from '../product-detail-summary/product-detail-summary';

@Component({
    selector: 'fd-product-detail',
    templateUrl: './product-detail.html',
    styleUrls: ['./product-detail.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProductDetailFacade],
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiDialogComponent,
        FdUiDialogHeaderDirective,
        FdUiButtonComponent,

        ProductDetailSummaryComponent,
        ProductDetailActionsComponent,
    ],
})
export class ProductDetailComponent {
    private readonly productDetailFacade = inject(ProductDetailFacade);
    private readonly quickMeal = inject(QuickMealService);
    protected addToMeal(): void {
        this.quickMeal.addProduct(this.product);
        this.close();
    }

    protected readonly isFavorite = this.productDetailFacade.isFavorite;
    protected readonly isFavoriteLoading = this.productDetailFacade.isFavoriteLoading;
    protected readonly isDuplicateInProgress = this.productDetailFacade.isDuplicateInProgress;

    protected product: Product;
    protected readonly productTypeKey: string;
    protected readonly baseUnitKey: string;

    protected calories: number;
    protected readonly qualityScore: number;
    protected readonly qualityGrade: string;
    protected readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    protected readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'PRODUCT_DETAIL.REMOVE_FAVORITE' : 'PRODUCT_DETAIL.ADD_FAVORITE',
    );
    protected readonly isDeleteDisabled = computed(() => !this.product.isOwnedByCurrentUser || this.product.usageCount > 0);
    protected readonly isEditDisabled = computed(() => !this.product.isOwnedByCurrentUser || this.product.usageCount > 0);
    protected readonly canModify = computed(() => !this.isEditDisabled());
    protected readonly warningMessage = computed(() => {
        if (!this.isDeleteDisabled() && !this.isEditDisabled()) {
            return null;
        }

        return this.product.isOwnedByCurrentUser ? 'PRODUCT_DETAIL.WARNING_MESSAGE' : 'PRODUCT_DETAIL.WARNING_NOT_OWNER';
    });
    protected readonly macroBlocks: ProductDetailMacroBlock[];
    protected readonly macroSummaryBlocks: ProductDetailMacroBlock[];
    protected readonly nutritionForm: FieldTree<NutritionFormModel>;
    protected readonly macroBarState: NutritionMacroState;
    public constructor() {
        this.product = inject<Product>(FD_UI_DIALOG_DATA);
        this.productDetailFacade.initialize(this.product);
        this.productTypeKey = buildProductTypeTranslationKey(this.product.productType ?? this.product.category ?? null);
        this.baseUnitKey = `GENERAL.UNITS.${this.product.baseUnit}`;
        this.qualityScore = normalizeQualityScore(this.product.qualityScore);
        this.qualityGrade = this.product.qualityGrade;
        this.calories = this.product.caloriesPerBase;

        const nutritionViewModel = buildProductDetailNutritionViewModel(this.product, inject(ChartColorsService).palette);
        this.nutritionForm = form(signal(nutritionViewModel.nutritionModel));
        this.macroBarState = nutritionViewModel.macroBarState;
        this.macroBlocks = nutritionViewModel.macroBlocks;
        this.macroSummaryBlocks = nutritionViewModel.macroSummaryBlocks;
    }

    protected close(): void {
        this.productDetailFacade.close(this.product);
    }

    protected onEdit(): void {
        if (this.isEditDisabled()) {
            return;
        }
        this.productDetailFacade.edit(this.product);
    }

    protected onDelete(): void {
        if (this.isDeleteDisabled()) {
            return;
        }
        this.productDetailFacade.delete(this.product);
    }

    protected onDuplicate(): void {
        this.productDetailFacade.duplicate(this.product);
    }

    protected toggleFavorite(): void {
        this.productDetailFacade.toggleFavorite(this.product);
    }
}

export { ProductDetailActionResult } from '../product-detail-lib/product-detail.types';
