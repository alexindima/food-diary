import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
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
import { of, switchMap } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import {
    type NutritionControlNames,
    NutritionEditorComponent,
    type NutritionMacroState,
} from '../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { CHART_COLORS } from '../../../../constants/chart-colors';
import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { normalizeQualityScore } from '../../../../shared/lib/quality-score.utils';
import type { NutrientData } from '../../../../shared/models/charts.data';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { ProductService } from '../../api/product.service';
import { buildProductTypeTranslationKey } from '../../lib/product-type.utils';
import type { Product } from '../../models/product.data';

const MACRO_SUMMARY_LIMIT = 3;
const MIN_MACRO_BAR_PERCENT = 4;
const MIN_MACRO_REFERENCE_VALUE = 1;

@Component({
    selector: 'fd-product-detail',
    standalone: true,
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
    public activeTab: 'summary' | 'nutrients' = 'summary';
    public readonly onTabChange = (tab: string): void => {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    };

    public calories: number;
    public readonly qualityScore: number;
    public readonly qualityGrade: string;
    public readonly qualityHintKey: string;
    public nutrientChartData: NutrientData;
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
    public readonly macroBlocks: Array<{
        labelKey: string;
        value: number;
        unitKey: string;
        color: string;
        percent: number;
    }>;
    public readonly macroSummaryBlocks = computed(() => this.macroBlocks.slice(0, MACRO_SUMMARY_LIMIT));
    public readonly nutritionControlNames: NutritionControlNames = {
        calories: 'calories',
        proteins: 'proteins',
        fats: 'fats',
        carbs: 'carbs',
        fiber: 'fiber',
        alcohol: 'alcohol',
    };
    public readonly nutritionForm: FormGroup;
    public readonly macroBarState: NutritionMacroState;
    public isDuplicateInProgress = false;

    public constructor() {
        const data = inject<Product>(FD_UI_DIALOG_DATA);

        this.product = data;
        this.initialFavoriteState = this.product.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteProductId = this.product.favoriteProductId ?? null;
        this.productTypeKey = buildProductTypeTranslationKey(this.product.productType ?? this.product.category ?? null);
        this.baseUnitKey = `GENERAL.UNITS.${this.product.baseUnit}`;
        this.qualityScore = normalizeQualityScore(this.product.qualityScore);
        this.qualityGrade = this.product.qualityGrade;
        this.qualityHintKey = `QUALITY.${this.qualityGrade.toUpperCase()}`;

        this.calories = this.product.caloriesPerBase;
        this.nutrientChartData = {
            proteins: this.product.proteinsPerBase,
            fats: this.product.fatsPerBase,
            carbs: this.product.carbsPerBase,
        };
        const datasetValues = [this.product.proteinsPerBase, this.product.fatsPerBase, this.product.carbsPerBase];
        this.nutritionForm = this.buildNutritionForm({
            calories: this.product.caloriesPerBase,
            proteins: this.product.proteinsPerBase,
            fats: this.product.fatsPerBase,
            carbs: this.product.carbsPerBase,
            fiber: this.product.fiberPerBase,
            alcohol: this.product.alcoholPerBase,
        });
        this.macroBarState = this.buildMacroBarState(datasetValues);
        this.macroBlocks = [
            {
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                value: this.product.proteinsPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.proteins,
                percent: this.resolveMacroPercent(this.product.proteinsPerBase, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                value: this.product.fatsPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fats,
                percent: this.resolveMacroPercent(this.product.fatsPerBase, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                value: this.product.carbsPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.carbs,
                percent: this.resolveMacroPercent(this.product.carbsPerBase, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FIBER',
                value: this.product.fiberPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fiber,
                percent: this.resolveMacroPercent(this.product.fiberPerBase, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.ALCOHOL',
                value: this.product.alcoholPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.alcohol,
                percent: this.resolveMacroPercent(this.product.alcoholPerBase, datasetValues),
            },
        ];
        this.favoriteProductService.isFavorite(this.product.id).subscribe(isFav => {
            this.initialFavoriteState = isFav;
            this.isFavorite.set(isFav);
        });
    }

    private buildNutritionForm(values: {
        calories: number;
        proteins: number;
        fats: number;
        carbs: number;
        fiber: number;
        alcohol: number;
    }): FormGroup {
        return new FormGroup({
            calories: new FormControl(values.calories),
            proteins: new FormControl(values.proteins),
            fats: new FormControl(values.fats),
            carbs: new FormControl(values.carbs),
            fiber: new FormControl(values.fiber),
            alcohol: new FormControl(values.alcohol),
        });
    }

    private buildMacroBarState(values: number[]): NutritionMacroState {
        const total = values.reduce((sum, value) => sum + value, 0);

        return {
            isEmpty: total <= 0,
            segments: [
                { key: 'proteins', percent: total > 0 ? (values[0] / total) * PERCENT_MULTIPLIER : 0 },
                { key: 'fats', percent: total > 0 ? (values[1] / total) * PERCENT_MULTIPLIER : 0 },
                { key: 'carbs', percent: total > 0 ? (values[2] / total) * PERCENT_MULTIPLIER : 0 },
            ],
        };
    }

    private resolveMacroPercent(value: number, values: number[]): number {
        const max = Math.max(...values, value, MIN_MACRO_REFERENCE_VALUE);
        return Math.max(MIN_MACRO_BAR_PERCENT, Math.round((value / max) * PERCENT_MULTIPLIER));
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
            .subscribe(confirm => {
                if (confirm === true) {
                    const deleteResult = new ProductDetailActionResult(this.product.id, 'Delete', this.hasFavoriteChanged());
                    this.dialogRef.close(deleteResult);
                }
            });
    }

    public onDuplicate(): void {
        if (this.isDuplicateInProgress) {
            return;
        }

        this.isDuplicateInProgress = true;
        this.productService.duplicate(this.product.id).subscribe({
            next: duplicated => {
                this.dialogRef.close(new ProductDetailActionResult(duplicated.id, 'Duplicate', this.hasFavoriteChanged()));
            },
            error: () => {
                this.isDuplicateInProgress = false;
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
                this.favoriteProductService.remove(this.favoriteProductId).subscribe({
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
            this.favoriteProductService.add(this.product.id).subscribe({
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

export class ProductDetailActionResult {
    public constructor(
        public id: string,
        public action: ProductDetailAction,
        public favoriteChanged = false,
    ) {}
}

export type ProductDetailAction = 'Edit' | 'Delete' | 'Duplicate' | 'FavoriteChanged';
