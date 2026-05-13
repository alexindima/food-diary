import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import type { FormGroup } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

import {
    type NutritionControlNames,
    NutritionEditorComponent,
    type NutritionMacroState,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { normalizeQualityScore } from '../../../../../shared/lib/quality-score.utils';
import { buildProductTypeTranslationKey } from '../../../lib/product-type.utils';
import type { Product } from '../../../models/product.data';
import { ProductDetailActionsComponent } from '../product-detail-actions/product-detail-actions.component';
import { ProductDetailFacade } from '../product-detail-lib/product-detail.facade';
import type { ProductDetailTab } from '../product-detail-lib/product-detail.types';
import {
    buildProductDetailNutritionViewModel,
    type ProductDetailMacroBlock,
    type ProductDetailNutritionForm,
} from '../product-detail-lib/product-detail-nutrition.mapper';
import { ProductDetailSummaryComponent } from '../product-detail-summary/product-detail-summary.component';

@Component({
    selector: 'fd-product-detail',
    templateUrl: './product-detail.component.html',
    styleUrls: ['./product-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProductDetailFacade],
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiDialogComponent,
        FdUiDialogHeaderDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        NutritionEditorComponent,
        ProductDetailSummaryComponent,
        ProductDetailActionsComponent,
    ],
})
export class ProductDetailComponent {
    private readonly productDetailFacade = inject(ProductDetailFacade);

    public readonly isFavorite = this.productDetailFacade.isFavorite;
    public readonly isFavoriteLoading = this.productDetailFacade.isFavoriteLoading;
    public readonly isDuplicateInProgress = this.productDetailFacade.isDuplicateInProgress;

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
    public constructor() {
        this.product = inject<Product>(FD_UI_DIALOG_DATA);
        this.productDetailFacade.initialize(this.product);
        this.productTypeKey = buildProductTypeTranslationKey(this.product.productType ?? this.product.category ?? null);
        this.baseUnitKey = `GENERAL.UNITS.${this.product.baseUnit}`;
        this.qualityScore = normalizeQualityScore(this.product.qualityScore);
        this.qualityGrade = this.product.qualityGrade;
        this.calories = this.product.caloriesPerBase;

        const nutritionViewModel = buildProductDetailNutritionViewModel(this.product);
        this.nutritionForm = nutritionViewModel.nutritionForm;
        this.macroBarState = nutritionViewModel.macroBarState;
        this.macroBlocks = nutritionViewModel.macroBlocks;
        this.macroSummaryBlocks = nutritionViewModel.macroSummaryBlocks;
    }

    public close(): void {
        this.productDetailFacade.close(this.product);
    }

    public onEdit(): void {
        if (this.isEditDisabled()) {
            return;
        }
        this.productDetailFacade.edit(this.product);
    }

    public onDelete(): void {
        if (this.isDeleteDisabled()) {
            return;
        }
        this.productDetailFacade.delete(this.product);
    }

    public onDuplicate(): void {
        this.productDetailFacade.duplicate(this.product);
    }

    public toggleFavorite(): void {
        this.productDetailFacade.toggleFavorite(this.product);
    }
}

export { ProductDetailActionResult } from '../product-detail-lib/product-detail.types';
