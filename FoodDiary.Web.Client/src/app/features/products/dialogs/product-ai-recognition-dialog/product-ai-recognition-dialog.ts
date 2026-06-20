import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { disabled as disabledRule, form, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { catchError, of } from 'rxjs';

import { ImageUploadFieldComponent } from '../../../../components/shared/image-upload-field/image-upload-field';
import { FrontendLoggerService } from '../../../../services/frontend-logger.service';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { ProductAiRecognitionFacade } from '../../lib/product-ai-recognition.facade';
import { ProductAiRecognitionActionComponent } from './product-ai-recognition-action/product-ai-recognition-action';
import type { ProductAiDialogData, ProductAiRecognitionResult } from './product-ai-recognition-dialog.types';
import {
    buildProductAiRecognitionModelFromNutrition,
    buildProductAiRecognitionResult,
    capitalizeName,
    createProductAiRecognitionFormModel,
    mapAiNutritionErrorKey,
    mapAiRecognitionErrorKey,
    normalizeItemsForNutrition,
} from './product-ai-recognition-lib/product-ai-recognition.helpers';
import { ProductAiRecognitionResultComponent } from './product-ai-recognition-result/product-ai-recognition-result';

@Component({
    selector: 'fd-product-ai-recognition-dialog',
    templateUrl: './product-ai-recognition-dialog.html',
    styleUrls: ['./product-ai-recognition-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiTextareaComponent,
        ImageUploadFieldComponent,
        ProductAiRecognitionActionComponent,
        ProductAiRecognitionResultComponent,
    ],
})
export class ProductAiRecognitionDialogComponent {
    private readonly dialogData = inject<ProductAiDialogData>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    private readonly dialogRef = inject(FdUiDialogRef<ProductAiRecognitionDialogComponent, ProductAiRecognitionResult | null>, {
        optional: true,
    });
    private readonly productAiRecognitionFacade = inject(ProductAiRecognitionFacade);
    private readonly logger = inject(FrontendLoggerService);
    protected readonly isLoading = signal(false);
    protected readonly isNutritionLoading = signal(false);
    protected readonly hasAnalyzed = signal(false);
    protected readonly errorKey = signal<string | null>(null);
    protected readonly nutritionErrorKey = signal<string | null>(null);
    protected readonly selection = signal<ImageSelection | null>(null);
    protected readonly results = signal<FoodVisionItem[]>([]);
    protected readonly nutrition = signal<FoodNutritionResponse | null>(null);
    protected readonly descriptionModel = signal({ description: this.dialogData.initialDescription ?? '' });
    protected readonly descriptionForm = form(this.descriptionModel, path => {
        disabledRule(path.description, { when: () => this.isLoading() || this.isNutritionLoading() });
    });
    protected readonly resultFormModel = signal(createProductAiRecognitionFormModel());
    protected readonly resultForm = form(this.resultFormModel);

    protected readonly statusKey = computed(() => {
        if (this.selection() === null) {
            return null;
        }
        if (this.isLoading()) {
            return 'PRODUCT_AI_DIALOG.STATUS_ANALYZING';
        }
        if (this.isNutritionLoading()) {
            return 'PRODUCT_AI_DIALOG.STATUS_NUTRITION';
        }
        if (this.hasAnalyzed()) {
            return 'PRODUCT_AI_DIALOG.STATUS_DONE';
        }
        return null;
    });
    protected readonly canApply = computed(() => this.nutrition() !== null);
    protected readonly isAnalyzeDisabled = computed(() => this.selection() === null || this.isLoading() || this.isNutritionLoading());
    protected readonly analyzeDisabledReason = computed(() => {
        if (this.selection() === null) {
            return 'DISABLED_HINTS.IMAGE_REQUIRED';
        }

        if (this.isLoading() || this.isNutritionLoading()) {
            return 'DISABLED_HINTS.OPERATION_BUSY';
        }

        return null;
    });
    protected readonly itemNames = computed(() =>
        this.results()
            .map(item => capitalizeName(item.nameLocal?.trim() ?? item.nameEn.trim()))
            .filter(name => name.length > 0),
    );
    protected onImageChanged(selection: ImageSelection | null): void {
        this.selection.set(selection);
        this.errorKey.set(null);
        this.nutritionErrorKey.set(null);
        this.results.set([]);
        this.nutrition.set(null);
        this.hasAnalyzed.set(false);
    }

    protected startAnalysis(): void {
        this.runAnalysisFlow();
    }

    protected reanalyze(): void {
        this.runAnalysisFlow();
    }

    private runAnalysisFlow(): void {
        if (this.isLoading() || this.isNutritionLoading()) {
            return;
        }

        const assetId = this.selection()?.assetId;
        if (assetId === undefined || assetId === null || assetId.length === 0) {
            return;
        }

        this.errorKey.set(null);
        this.nutritionErrorKey.set(null);
        this.results.set([]);
        this.nutrition.set(null);
        this.hasAnalyzed.set(false);
        this.runAnalysis(assetId);
    }

    protected apply(): void {
        const nutrition = this.nutrition();
        if (nutrition === null) {
            return;
        }

        const result = buildProductAiRecognitionResult({
            model: this.resultFormModel(),
            selection: this.selection(),
            itemNames: this.itemNames(),
            results: this.results(),
            description: this.getDescription(),
        });

        this.dialogRef?.close(result);
    }

    protected close(): void {
        this.cleanupAsset();
        this.dialogRef?.close(null);
    }

    private runAnalysis(assetId: string): void {
        this.isLoading.set(true);
        this.productAiRecognitionFacade
            .analyzeFoodImage({
                imageAssetId: assetId,
                description: this.getDescription(),
            })
            .pipe(
                catchError((err: unknown) => {
                    this.errorKey.set(mapAiRecognitionErrorKey(err));
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.isLoading.set(false);
                this.hasAnalyzed.set(true);
                if (response === null) {
                    return;
                }
                const items = response.items;
                this.results.set(items);
                if (items.length > 0) {
                    this.runNutrition(items);
                }
            });
    }

    private runNutrition(items: FoodVisionItem[]): void {
        this.isNutritionLoading.set(true);
        this.nutritionErrorKey.set(null);
        const normalizedItems = normalizeItemsForNutrition(items);
        this.productAiRecognitionFacade
            .calculateNutrition({ items: normalizedItems })
            .pipe(
                catchError((err: unknown) => {
                    this.nutritionErrorKey.set(mapAiNutritionErrorKey(err));
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.isNutritionLoading.set(false);
                if (response === null) {
                    return;
                }
                this.nutrition.set(response);
                this.resultFormModel.set(buildProductAiRecognitionModelFromNutrition(items, response));
            });
    }

    private getDescription(): string | null {
        const value = this.descriptionModel().description.trim();
        return value.length > 0 ? value : null;
    }

    private cleanupAsset(): void {
        const assetId = this.selection()?.assetId;
        if (assetId === undefined || assetId === null || assetId.length === 0) {
            return;
        }

        this.productAiRecognitionFacade.deleteAsset(assetId).subscribe({
            error: (err: unknown) => {
                this.logger.warn('Failed to delete AI product image asset', err);
            },
        });
    }
}
