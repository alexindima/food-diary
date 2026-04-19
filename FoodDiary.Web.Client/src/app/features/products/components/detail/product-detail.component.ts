import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, TooltipItem } from 'chart.js';
import { MatIconModule } from '@angular/material/icon';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { CHART_COLORS } from '../../../../constants/chart-colors';
import { ProductService } from '../../api/product.service';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { Product } from '../../models/product.data';
import { buildProductTypeTranslationKey } from '../../lib/product-type.utils';
import { NutrientData } from '../../../../shared/models/charts.data';
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';

@Component({
    selector: 'fd-product-detail',
    standalone: true,
    templateUrl: './product-detail.component.html',
    styleUrls: ['./product-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        FdUiAccentSurfaceComponent,
        BaseChartDirective,
        MatIconModule,
    ],
})
export class ProductDetailComponent {
    private readonly productService = inject(ProductService);
    private readonly favoriteProductService = inject(FavoriteProductService);
    private readonly dialogRef = inject(FdUiDialogRef<ProductDetailComponent>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    private favoriteProductId: string | null = null;

    public product: Product;
    public readonly productTypeKey: string;
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
    public nutrientChartData: NutrientData;
    public pieChartData: ChartData<'pie', number[], string>;
    public barChartData: ChartData<'bar', number[], string>;
    public pieChartOptions: ChartOptions<'pie'>;
    public barChartOptions: ChartOptions<'bar'>;
    public readonly chartSize = 200;
    public readonly isDeleteDisabled = computed(() => !this.product.isOwnedByCurrentUser || this.product.usageCount > 0);
    public readonly isEditDisabled = computed(() => !this.product.isOwnedByCurrentUser || this.product.usageCount > 0);
    public readonly canModify = computed(() => !this.isEditDisabled());
    public readonly warningMessage = computed(() => {
        if (!this.isDeleteDisabled() && !this.isEditDisabled()) {
            return null;
        }

        return this.product.isOwnedByCurrentUser ? 'PRODUCT_DETAIL.WARNING_MESSAGE' : 'PRODUCT_DETAIL.WARNING_NOT_OWNER';
    });
    public readonly macroBlocks: {
        labelKey: string;
        value: number;
        unitKey: string;
        color: string;
    }[];
    public isDuplicateInProgress = false;

    public constructor() {
        const data = inject<Product>(FD_UI_DIALOG_DATA);

        this.product = data;
        this.productTypeKey = buildProductTypeTranslationKey(this.product.productType ?? this.product.category ?? null);

        this.calories = this.product.caloriesPerBase;
        this.nutrientChartData = {
            proteins: this.product.proteinsPerBase,
            fats: this.product.fatsPerBase,
            carbs: this.product.carbsPerBase,
        };
        const labels = [
            this.translate.instant('GENERAL.NUTRIENTS.PROTEIN'),
            this.translate.instant('GENERAL.NUTRIENTS.FAT'),
            this.translate.instant('GENERAL.NUTRIENTS.CARB'),
        ];
        const datasetValues = [this.product.proteinsPerBase, this.product.fatsPerBase, this.product.carbsPerBase];
        const colors = [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs];
        this.pieChartData = {
            labels,
            datasets: [
                {
                    data: datasetValues,
                    backgroundColor: colors,
                },
            ],
        };
        this.barChartData = {
            labels,
            datasets: [
                {
                    data: datasetValues,
                    backgroundColor: colors,
                },
            ],
        };
        const tooltipLabel = (label: string, value: number): string =>
            `${label}: ${value.toFixed(2)} ${this.translate.instant('GENERAL.UNITS.G')}`;
        this.pieChartOptions = {
            responsive: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: (ctx: TooltipItem<'pie'>): string => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
                    },
                },
            },
        };
        this.barChartOptions = {
            responsive: true,
            scales: {
                x: { display: false },
                y: { beginAtZero: true },
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: (ctx: TooltipItem<'bar'>): string => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
                    },
                },
            },
        };
        this.macroBlocks = [
            {
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                value: this.product.proteinsPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.proteins,
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                value: this.product.fatsPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fats,
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                value: this.product.carbsPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.carbs,
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FIBER',
                value: this.product.fiberPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fiber,
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.ALCOHOL',
                value: this.product.alcoholPerBase,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.alcohol,
            },
        ];
        this.favoriteProductService.isFavorite(this.product.id).subscribe(isFav => this.isFavorite.set(isFav));
    }

    public onEdit(): void {
        if (this.isEditDisabled()) {
            return;
        }
        const editResult = new ProductDetailActionResult(this.product.id, 'Edit');
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
                if (confirm) {
                    const deleteResult = new ProductDetailActionResult(this.product.id, 'Delete');
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
                this.dialogRef.close(new ProductDetailActionResult(duplicated.id, 'Duplicate'));
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
            if (this.favoriteProductId) {
                this.favoriteProductService.remove(this.favoriteProductId).subscribe({
                    next: () => {
                        this.isFavorite.set(false);
                        this.favoriteProductId = null;
                        this.isFavoriteLoading.set(false);
                    },
                    error: () => this.isFavoriteLoading.set(false),
                });
                return;
            }

            this.favoriteProductService.getAll().subscribe({
                next: favorites => {
                    const match = favorites.find(f => f.productId === this.product.id);
                    if (match) {
                        this.favoriteProductService.remove(match.id).subscribe({
                            next: () => {
                                this.isFavorite.set(false);
                                this.favoriteProductId = null;
                                this.isFavoriteLoading.set(false);
                            },
                            error: () => this.isFavoriteLoading.set(false),
                        });
                    } else {
                        this.isFavorite.set(false);
                        this.isFavoriteLoading.set(false);
                    }
                },
                error: () => this.isFavoriteLoading.set(false),
            });
        } else {
            this.favoriteProductService.add(this.product.id).subscribe({
                next: favorite => {
                    this.isFavorite.set(true);
                    this.favoriteProductId = favorite.id;
                    this.isFavoriteLoading.set(false);
                },
                error: () => this.isFavoriteLoading.set(false),
            });
        }
    }
}

export class ProductDetailActionResult {
    public constructor(
        public id: string,
        public action: ProductDetailAction,
    ) {}
}

export type ProductDetailAction = 'Edit' | 'Delete' | 'Duplicate';
