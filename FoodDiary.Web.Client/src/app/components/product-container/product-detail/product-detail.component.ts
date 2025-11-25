import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Product } from '../../../types/product.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    NutrientsSummaryComponent,
    NutrientsSummaryConfig,
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { NutrientData } from '../../../types/charts.data';
import { ProductService } from '../../../services/product.service';
import { buildProductTypeTranslationKey } from '../../../utils/product-type.utils';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    FdUiConfirmDialogComponent,
    FdUiConfirmDialogData,
} from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';

@Component({
    selector: 'fd-product-detail',
    standalone: true,
    templateUrl: './product-detail.component.html',
    styleUrls: ['./product-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        NutrientsSummaryComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
    ],
})
export class ProductDetailComponent {
    private readonly productService = inject(ProductService);
    private readonly dialogRef = inject(FdUiDialogRef<ProductDetailComponent>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);

    public product: Product;
    public readonly productTypeKey: string;

    public readonly nutrientSummaryConfig: NutrientsSummaryConfig = {
        styles: {
            common: {
                infoBreakpoints: {
                    columnLayout: 680,
                },
            },
            charts: {
                chartBlockSize: 160,
                breakpoints: {
                    columnLayout: 680,
                },
            },
        },
    };

    public calories: number;
    public nutrientChartData: NutrientData;
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
        const data: FdUiConfirmDialogData = {
            title: this.translate.instant('PRODUCT_DETAIL.CONFIRM_DELETE_TITLE'),
            message: this.product.name,
            confirmLabel: this.translate.instant('PRODUCT_DETAIL.CONFIRM_BUTTON'),
            cancelLabel: this.translate.instant('PRODUCT_DETAIL.CANCEL_BUTTON'),
            danger: true,
        };

        this.fdDialogService
            .open(FdUiConfirmDialogComponent, { data, size: 'sm' })
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
