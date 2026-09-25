import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { disabled as disabledRule, form, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { catchError, of, type Subscription } from 'rxjs';

import { FoodRecognitionHistoryComponent } from '../../../../components/shared/food-recognition-history/food-recognition-history';
import type { FoodNutritionResponse, FoodVisionItem, FoodVisionResponse, ProductLabel } from '../../../../shared/models/ai.data';
import type { FoodRecognitionJob } from '../../../../shared/models/food-recognition.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { ProductAiRecognitionFacade } from '../../lib/product-ai-recognition.facade';
import type { ProductAiDialogData, ProductAiRecognitionResult } from './product-ai-recognition-dialog.types';
import {
    buildProductAiRecognitionModelFromNutrition,
    buildProductAiRecognitionResult,
    buildProductLabelFormModel,
    capitalizeName,
    createProductAiRecognitionFormModel,
    isProductAiRecognitionModelValid,
    mapAiNutritionErrorKey,
    mapAiRecognitionErrorKey,
    normalizeItemsForNutrition,
} from './product-ai-recognition-lib/product-ai-recognition.helpers';
import { ProductAiRecognitionResultComponent } from './product-ai-recognition-result/product-ai-recognition-result';
import { ProductRecognitionPhotosComponent } from './product-recognition-photos/product-recognition-photos';

@Component({
    selector: 'fd-product-ai-recognition-dialog',
    templateUrl: './product-ai-recognition-dialog.html',
    styleUrls: ['./product-ai-recognition-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FoodRecognitionHistoryComponent,
        FormField,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiHintDirective,
        FdUiDialogHeaderDirective,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiTextareaComponent,
        ProductRecognitionPhotosComponent,
        ProductAiRecognitionResultComponent,
    ],
})
export class ProductAiRecognitionDialogComponent {
    private readonly dialogData = inject<ProductAiDialogData>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    private readonly dialogRef = inject(FdUiDialogRef<ProductAiRecognitionDialogComponent, ProductAiRecognitionResult | null>, {
        optional: true,
    });
    private readonly productAiRecognitionFacade = inject(ProductAiRecognitionFacade);
    private readonly destroyRef = inject(DestroyRef);
    private analysisSubscription: Subscription | null = null;
    private nutritionSubscription: Subscription | null = null;
    protected readonly historyOpen = signal(false);
    protected readonly isLoading = signal(false);
    protected readonly isNutritionLoading = signal(false);
    protected readonly hasAnalyzed = signal(false);
    protected readonly errorKey = signal<string | null>(null);
    protected readonly nutritionErrorKey = signal<string | null>(null);
    protected readonly useAsCover = signal(false);
    protected readonly replacementAccepted = signal(false);
    protected readonly hasExistingData = this.dialogData.hasExistingData ?? false;
    protected readonly isBusy = computed(() => this.isLoading() || this.isNutritionLoading());
    protected readonly photoUploading = signal(false);
    protected readonly photos = signal<ImageSelection[]>([...(this.dialogData.initialPhotos ?? [])]);
    protected readonly cover = signal<ImageSelection | null>(this.photos()[0] ?? null);
    protected readonly productLabel = signal<ProductLabel | null>(null);
    protected readonly hasResult = computed(() => this.productLabel() !== null || this.nutrition() !== null);
    protected readonly selection = signal<ImageSelection | null>(this.photos()[0] ?? null);
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
        if (this.hasResult()) {
            return 'PRODUCT_AI_DIALOG.STATUS_DONE';
        }
        return null;
    });
    protected readonly isResultValid = computed(() => isProductAiRecognitionModelValid(this.resultFormModel()));
    protected readonly canApply = computed(
        () => this.hasResult() && !this.isBusy() && this.isResultValid() && (!this.hasExistingData || this.replacementAccepted()),
    );
    protected readonly isEmpty = computed(
        () =>
            this.hasAnalyzed() &&
            !this.isBusy() &&
            this.errorKey() === null &&
            this.nutritionErrorKey() === null &&
            this.results().length === 0 &&
            !this.hasResult(),
    );
    protected readonly isAnalyzeDisabled = computed(
        () => (this.selection()?.assetId ?? '').length === 0 || this.isBusy() || this.photoUploading(),
    );
    protected readonly analyzeDisabledReason = computed(() => {
        if (this.selection() === null) {
            return 'DISABLED_HINTS.IMAGE_REQUIRED';
        }

        if (this.isBusy() || this.photoUploading()) {
            return 'DISABLED_HINTS.OPERATION_BUSY';
        }

        return null;
    });
    protected readonly notices = computed(() =>
        [
            this.isBusy() ? 'PRODUCT_AI_DIALOG.PROCESSING_HINT' : null,
            this.isEmpty() ? 'PRODUCT_AI_DIALOG.EMPTY' : null,
            this.errorKey(),
            this.nutritionErrorKey(),
        ].filter((key): key is string => key !== null),
    );
    protected readonly itemNames = computed(() =>
        this.results()
            .map(item => capitalizeName(item.nameLocal?.trim() ?? item.nameEn.trim()))
            .filter(name => name.length > 0),
    );
    protected onImageChanged(selection: ImageSelection | null): void {
        this.analysisSubscription?.unsubscribe();
        this.nutritionSubscription?.unsubscribe();
        this.isLoading.set(false);
        this.isNutritionLoading.set(false);
        this.selection.set(selection);
        this.productLabel.set(null);
        this.useAsCover.set(false);
        this.replacementAccepted.set(false);
        this.errorKey.set(null);
        this.nutritionErrorKey.set(null);
        this.results.set([]);
        this.nutrition.set(null);
        this.hasAnalyzed.set(false);
    }

    protected onPhotosChanged(photos: ImageSelection[]): void {
        if (this.isBusy()) {
            return;
        }
        this.photos.set(photos);
        this.cover.set(null);
        this.onImageChanged(photos[0] ?? null);
    }

    protected onCoverChanged(photo: ImageSelection | null): void {
        if (photo !== null) {
            this.photos.update(photos => [photo, ...photos.filter(item => item.assetId !== photo.assetId)]);
        }
        this.cover.set(photo);
        this.useAsCover.set(photo !== null);
    }

    protected startAnalysis(): void {
        this.runAnalysisFlow();
    }

    protected refineRecognition(): void {
        if (this.isBusy()) {
            return;
        }
        this.productLabel.set(null);
        this.nutrition.set(null);
        this.results.set([]);
        this.hasAnalyzed.set(false);
        this.replacementAccepted.set(false);
        this.errorKey.set(null);
        this.nutritionErrorKey.set(null);
    }

    protected reanalyze(): void {
        this.runAnalysisFlow();
    }

    private runAnalysisFlow(): void {
        if (this.isAnalyzeDisabled()) {
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
        if (!this.canApply()) {
            return;
        }

        const result = buildProductAiRecognitionResult({
            model: this.resultFormModel(),
            selection: this.useAsCover() ? (this.cover() ?? this.selection()) : null,
            itemNames: this.itemNames(),
            results: this.results(),
            description: null,
        });

        this.dialogRef?.close({ ...result, images: this.photos() });
    }

    protected close(): void {
        this.dialogRef?.close(null);
    }

    protected onResumeRecognition(job: FoodRecognitionJob): void {
        this.historyOpen.set(false);
        this.useAsCover.set(false);
        this.replacementAccepted.set(false);
        const selection = { assetId: job.imageAssetId, url: job.imageUrl };
        this.photos.set([selection, ...(job.additionalImages ?? []).map(image => ({ assetId: image.imageAssetId, url: image.imageUrl }))]);
        this.cover.set(null);
        this.selection.set(selection);
        this.productLabel.set(null);
        this.descriptionModel.set({ description: job.description ?? '' });
        this.runAnalysis(job.imageAssetId, job.id);
    }

    private runAnalysis(assetId: string, jobId?: string): void {
        this.resetAnalysisState();
        this.subscribeToAnalysis(assetId, jobId);
    }

    private resetAnalysisState(): void {
        this.analysisSubscription?.unsubscribe();
        this.nutritionSubscription?.unsubscribe();
        this.isLoading.set(true);
        this.isNutritionLoading.set(false);
        this.results.set([]);
        this.nutritionErrorKey.set(null);
        this.errorKey.set(null);
        this.nutrition.set(null);
        this.hasAnalyzed.set(false);
        this.productLabel.set(null);
        this.replacementAccepted.set(false);
    }

    private subscribeToAnalysis(assetId: string, jobId?: string): void {
        this.analysisSubscription = (
            jobId === undefined
                ? this.productAiRecognitionFacade.analyzeFoodImage({
                      imageAssetId: assetId,
                      isProductLabel: true,
                      additionalImageAssetIds: this.photos()
                          .map(photo => photo.assetId)
                          .filter((id): id is string => id !== null && id !== assetId),
                      description: this.getDescription(),
                  })
                : this.productAiRecognitionFacade.resumeRecognition(jobId)
        )
            .pipe(
                catchError((err: unknown) => {
                    this.errorKey.set(mapAiRecognitionErrorKey(err));
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.applyRecognitionResponse(response);
            });
    }

    private applyRecognitionResponse(response: FoodVisionResponse | null): void {
        this.isLoading.set(false);
        this.hasAnalyzed.set(true);
        if (response === null) {
            return;
        }
        if (response.productLabel !== null && response.productLabel !== undefined) {
            this.productLabel.set(response.productLabel);
            this.resultFormModel.set(buildProductLabelFormModel(response.productLabel));
            return;
        }
        const items = response.items;
        this.results.set(items);
        if (response.recognition !== undefined) {
            const nutrition = response.recognition.nutrition;
            this.nutrition.set(nutrition);
            if (nutrition !== null) {
                this.resultFormModel.set(buildProductAiRecognitionModelFromNutrition(items, nutrition));
            }
            this.nutritionErrorKey.set(response.recognition.errorCode === null ? null : 'PRODUCT_AI_DIALOG.NUTRITION_ERROR');
        } else if (items.length > 0) {
            this.runNutrition(items);
        }
    }

    private runNutrition(items: FoodVisionItem[]): void {
        this.isNutritionLoading.set(true);
        this.nutritionErrorKey.set(null);
        const normalizedItems = normalizeItemsForNutrition(items);
        this.nutritionSubscription?.unsubscribe();
        this.nutritionSubscription = this.productAiRecognitionFacade
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

    public constructor() {
        this.destroyRef.onDestroy(() => {
            this.analysisSubscription?.unsubscribe();
            this.nutritionSubscription?.unsubscribe();
        });
    }
}
