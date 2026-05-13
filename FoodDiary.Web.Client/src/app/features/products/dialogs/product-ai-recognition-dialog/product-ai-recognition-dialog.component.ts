import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { catchError, of } from 'rxjs';

import { ImageUploadFieldComponent } from '../../../../components/shared/image-upload-field/image-upload-field.component';
import { FrontendLoggerService } from '../../../../services/frontend-logger.service';
import { AiFoodService } from '../../../../shared/api/ai-food.service';
import { ImageUploadService } from '../../../../shared/api/image-upload.service';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { ProductAiRecognitionActionComponent } from './product-ai-recognition-action/product-ai-recognition-action.component';
import type { ProductAiDialogData, ProductAiRecognitionFormGroup, ProductAiRecognitionResult } from './product-ai-recognition-dialog.types';
import {
    applyNutritionToProductAiRecognitionForm,
    buildProductAiRecognitionResult,
    capitalizeName,
    createProductAiRecognitionForm,
    mapAiNutritionErrorKey,
    mapAiRecognitionErrorKey,
    normalizeItemsForNutrition,
} from './product-ai-recognition-lib/product-ai-recognition.helpers';
import { ProductAiRecognitionResultComponent } from './product-ai-recognition-result/product-ai-recognition-result.component';

@Component({
    selector: 'fd-product-ai-recognition-dialog',
    templateUrl: './product-ai-recognition-dialog.component.html',
    styleUrls: ['./product-ai-recognition-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
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
    private readonly aiFoodService = inject(AiFoodService);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly logger = inject(FrontendLoggerService);
    public readonly isLoading = signal(false);
    public readonly isNutritionLoading = signal(false);
    public readonly hasAnalyzed = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly nutritionErrorKey = signal<string | null>(null);
    public readonly selection = signal<ImageSelection | null>(null);
    public readonly results = signal<FoodVisionItem[]>([]);
    public readonly nutrition = signal<FoodNutritionResponse | null>(null);
    public readonly descriptionControl = new FormControl(this.dialogData.initialDescription ?? '', { nonNullable: true });
    public readonly resultForm: ProductAiRecognitionFormGroup = createProductAiRecognitionForm();

    public constructor() {
        effect(() => {
            const disabled = this.isLoading() || this.isNutritionLoading();
            if (disabled) {
                this.descriptionControl.disable({ emitEvent: false });
            } else {
                this.descriptionControl.enable({ emitEvent: false });
            }
        });
    }

    public readonly statusKey = computed(() => {
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
    public readonly canApply = computed(() => this.nutrition() !== null);
    public readonly isAnalyzeDisabled = computed(() => this.selection() === null || this.isLoading() || this.isNutritionLoading());
    public readonly itemNames = computed(() =>
        this.results()
            .map(item => capitalizeName(item.nameLocal?.trim() ?? item.nameEn.trim()))
            .filter(name => name.length > 0),
    );
    public onImageChanged(selection: ImageSelection | null): void {
        this.selection.set(selection);
        this.errorKey.set(null);
        this.nutritionErrorKey.set(null);
        this.results.set([]);
        this.nutrition.set(null);
        this.hasAnalyzed.set(false);
    }

    public startAnalysis(): void {
        this.runAnalysisFlow();
    }

    public reanalyze(): void {
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

    public apply(): void {
        const nutrition = this.nutrition();
        if (nutrition === null) {
            return;
        }

        const result = buildProductAiRecognitionResult({
            form: this.resultForm,
            selection: this.selection(),
            itemNames: this.itemNames(),
            results: this.results(),
            description: this.getDescription(),
        });

        this.dialogRef?.close(result);
    }

    public close(): void {
        this.cleanupAsset();
        this.dialogRef?.close(null);
    }

    private runAnalysis(assetId: string): void {
        this.isLoading.set(true);
        this.aiFoodService
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
        this.aiFoodService
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
                applyNutritionToProductAiRecognitionForm(this.resultForm, items, response);
            });
    }

    private getDescription(): string | null {
        const value = this.descriptionControl.value.trim();
        return value.length > 0 ? value : null;
    }

    private cleanupAsset(): void {
        const assetId = this.selection()?.assetId;
        if (assetId === undefined || assetId === null || assetId.length === 0) {
            return;
        }

        this.imageUploadService.deleteAsset(assetId).subscribe({
            error: (err: unknown) => {
                this.logger.warn('Failed to delete AI product image asset', err);
            },
        });
    }
}
