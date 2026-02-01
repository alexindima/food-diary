import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Product } from '../../../types/product.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { NutrientData } from '../../../types/charts.data';
import { ProductService } from '../../../services/product.service';
import { buildProductTypeTranslationKey } from '../../../utils/product-type.utils';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { CHART_COLORS } from '../../../constants/chart-colors';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../../shared/confirm-delete-dialog/confirm-delete-dialog.component';

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
    ],
})
export class ProductDetailComponent {
    private readonly productService = inject(ProductService);
    private readonly dialogRef = inject(FdUiDialogRef<ProductDetailComponent>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);

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
    public readonly macroBlocks: {
        labelKey: string;
        value: number;
        unitKey: string;
        color: string;
    }[];
    public isDuplicateInProgress = false;

    public get isDeleteDisabled(): boolean {
        return !this.product.isOwnedByCurrentUser || this.product.usageCount > 0;
    }

    public get isEditDisabled(): boolean {
        return !this.product.isOwnedByCurrentUser || this.product.usageCount > 0;
    }

    public get canModify(): boolean {
        return !this.isEditDisabled;
    }

    public get warningMessage(): string | null {
        if (!this.isDeleteDisabled && !this.isEditDisabled) {
            return null;
        }

        return this.product.isOwnedByCurrentUser
            ? 'PRODUCT_DETAIL.WARNING_MESSAGE'
            : 'PRODUCT_DETAIL.WARNING_NOT_OWNER';
    }

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
        const datasetValues = [
            this.product.proteinsPerBase,
            this.product.fatsPerBase,
            this.product.carbsPerBase,
        ];
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
        const tooltipLabel = (label: string, value: number) =>
            `${label}: ${value.toFixed(2)} ${this.translate.instant('GENERAL.UNITS.G')}`;
        this.pieChartOptions = {
            responsive: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
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
                        label: ctx => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
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
    }

    public onEdit(): void {
        if (this.isEditDisabled) {
            return;
        }
        const editResult = new ProductDetailActionResult(this.product.id, 'Edit');
        this.dialogRef.close(editResult);
    }

    public onDelete(): void {
        if (this.isDeleteDisabled) {
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
}

export class ProductDetailActionResult {
    public constructor(
        public id: string,
        public action: ProductDetailAction,
    ) {}
}

export type ProductDetailAction = 'Edit' | 'Delete' | 'Duplicate';
