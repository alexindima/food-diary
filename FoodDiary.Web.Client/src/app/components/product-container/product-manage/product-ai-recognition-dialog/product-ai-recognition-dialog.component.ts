import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { ImageUploadFieldComponent } from '../../../shared/image-upload-field/image-upload-field.component';
import { AiFoodService } from '../../../../services/ai-food.service';
import { ImageUploadService } from '../../../../services/image-upload.service';
import { FoodNutritionResponse, FoodVisionItem } from '../../../../types/ai.data';
import { ImageSelection } from '../../../../types/image-upload.data';
import { MeasurementUnit } from '../../../../types/product.data';
import { catchError, of } from 'rxjs';

type ProductAiDialogData = {
    initialDescription?: string | null;
};

export type ProductAiRecognitionResult = {
    name: string;
    description?: string | null;
    baseAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
};

@Component({
    selector: 'fd-product-ai-recognition-dialog',
    standalone: true,
    templateUrl: './product-ai-recognition-dialog.component.html',
    styleUrls: ['./product-ai-recognition-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        ImageUploadFieldComponent,
    ],
})
export class ProductAiRecognitionDialogComponent {
    private readonly dialogData =
        inject<ProductAiDialogData>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    private readonly dialogRef = inject(
        FdUiDialogRef<ProductAiRecognitionDialogComponent, ProductAiRecognitionResult | null>,
        { optional: true },
    );
    private readonly aiFoodService = inject(AiFoodService);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly translateService = inject(TranslateService);

    public readonly isLoading = signal(false);
    public readonly isNutritionLoading = signal(false);
    public readonly hasAnalyzed = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly nutritionErrorKey = signal<string | null>(null);
    public readonly selection = signal<ImageSelection | null>(null);
    public readonly results = signal<FoodVisionItem[]>([]);
    public readonly nutrition = signal<FoodNutritionResponse | null>(null);
    public readonly descriptionControl = new FormControl(
        this.dialogData.initialDescription ?? '',
        { nonNullable: true },
    );
    public readonly resultForm = new FormGroup({
        name: new FormControl('', { nonNullable: true }),
        baseUnit: new FormControl(MeasurementUnit.G, { nonNullable: true }),
        caloriesPerBase: new FormControl(0, { nonNullable: true }),
        proteinsPerBase: new FormControl(0, { nonNullable: true }),
        fatsPerBase: new FormControl(0, { nonNullable: true }),
        carbsPerBase: new FormControl(0, { nonNullable: true }),
        fiberPerBase: new FormControl(0, { nonNullable: true }),
        alcoholPerBase: new FormControl(0, { nonNullable: true }),
    });

    public readonly unitOptions: FdUiSelectOption<MeasurementUnit>[] = Object.values(MeasurementUnit).map(unit => ({
        value: unit,
        label: this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${MeasurementUnit[unit]}`),
    }));

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
        if (!this.selection()) {
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
        if (!assetId) {
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
        if (!nutrition) {
            return;
        }

        const name = this.resultForm.controls.name.value.trim();
        const baseUnit = this.resultForm.controls.baseUnit.value;
        const baseAmount = this.getDefaultBaseAmount(baseUnit);

        const result: ProductAiRecognitionResult = {
            name: name || this.getFallbackName(),
            description: this.getDescription() || null,
            baseAmount: baseAmount > 0 ? baseAmount : 100,
            baseUnit,
            caloriesPerBase: this.getNumber(this.resultForm.controls.caloriesPerBase.value),
            proteinsPerBase: this.getNumber(this.resultForm.controls.proteinsPerBase.value),
            fatsPerBase: this.getNumber(this.resultForm.controls.fatsPerBase.value),
            carbsPerBase: this.getNumber(this.resultForm.controls.carbsPerBase.value),
            fiberPerBase: this.getNumber(this.resultForm.controls.fiberPerBase.value),
            alcoholPerBase: this.getNumber(this.resultForm.controls.alcoholPerBase.value),
        };

        this.cleanupAsset();
        this.dialogRef?.close(result);
    }

    public close(): void {
        this.cleanupAsset();
        this.dialogRef?.close(null);
    }

    public canApply(): boolean {
        return Boolean(this.nutrition());
    }

    public itemNames(): string[] {
        return this.results()
            .map(item => this.capitalizeName(item.nameLocal?.trim() || item.nameEn?.trim() || ''))
            .filter(Boolean);
    }

    public hasMultipleItems(): boolean {
        return this.results().length > 1;
    }

    private runAnalysis(assetId: string): void {
        this.isLoading.set(true);
        this.aiFoodService
            .analyzeFoodImage({
                imageAssetId: assetId,
                description: this.getDescription(),
            })
            .pipe(
                catchError(err => {
                    if (err?.status === 403) {
                        this.errorKey.set('PRODUCT_AI_DIALOG.ERROR_PREMIUM');
                    } else if (err?.status === 429) {
                        this.errorKey.set('PRODUCT_AI_DIALOG.ERROR_QUOTA');
                    } else {
                        this.errorKey.set('PRODUCT_AI_DIALOG.ERROR_GENERIC');
                    }
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.isLoading.set(false);
                this.hasAnalyzed.set(true);
                if (!response) {
                    return;
                }
                const items = response.items ?? [];
                this.results.set(items);
                if (items.length) {
                    this.runNutrition(items);
                }
            });
    }

    private runNutrition(items: FoodVisionItem[]): void {
        this.isNutritionLoading.set(true);
        this.nutritionErrorKey.set(null);
        const normalizedItems = this.normalizeItemsForNutrition(items);
        this.aiFoodService
            .calculateNutrition({ items: normalizedItems })
            .pipe(
                catchError(err => {
                    if (err?.status === 429) {
                        this.nutritionErrorKey.set('PRODUCT_AI_DIALOG.ERROR_QUOTA');
                    } else {
                        this.nutritionErrorKey.set('PRODUCT_AI_DIALOG.NUTRITION_ERROR');
                    }
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.isNutritionLoading.set(false);
                if (!response) {
                    return;
                }
                this.nutrition.set(response);
                this.applyNutritionToForm(items, response);
            });
    }

    private applyNutritionToForm(items: FoodVisionItem[], nutrition: FoodNutritionResponse): void {
        const primary = items[0];
        const name = this.capitalizeName(primary?.nameLocal?.trim() || primary?.nameEn?.trim() || '');
        const baseUnit = this.resolveUnit(primary?.unit);
        const baseAmount = this.getDefaultBaseAmount(baseUnit);

        this.resultForm.patchValue({
            name,
            baseUnit,
            caloriesPerBase: nutrition.calories ?? 0,
            proteinsPerBase: nutrition.protein ?? 0,
            fatsPerBase: nutrition.fat ?? 0,
            carbsPerBase: nutrition.carbs ?? 0,
            fiberPerBase: nutrition.fiber ?? 0,
            alcoholPerBase: nutrition.alcohol ?? 0,
        });
    }

    private resolveUnit(unit?: string | null): MeasurementUnit {
        if (!unit) {
            return MeasurementUnit.G;
        }
        const normalized = unit.trim().toLowerCase();
        if (['g', 'gram', 'grams', 'gr'].includes(normalized)) {
            return MeasurementUnit.G;
        }
        if (['ml', 'l', 'liter', 'liters'].includes(normalized)) {
            return MeasurementUnit.ML;
        }
        if (['pcs', 'pc', 'piece', 'pieces'].includes(normalized)) {
            return MeasurementUnit.PCS;
        }
        return MeasurementUnit.G;
    }

    private getDefaultBaseAmount(unit: MeasurementUnit): number {
        return unit === MeasurementUnit.PCS ? 1 : 100;
    }

    private normalizeItemsForNutrition(items: FoodVisionItem[]): FoodVisionItem[] {
        const isSingleItem = items.length === 1;
        return items.map(item => {
            const baseUnit = this.resolveUnit(item.unit);
            const normalizedUnit = baseUnit === MeasurementUnit.PCS ? 'pcs' : baseUnit.toLowerCase();
            const normalizedAmount = baseUnit === MeasurementUnit.PCS
                ? 1
                : isSingleItem
                    ? 100
                    : item.amount;

            return {
                ...item,
                amount: normalizedAmount,
                unit: normalizedUnit,
            };
        });
    }

    private getDescription(): string | null {
        const value = this.descriptionControl.value?.trim();
        return value ? value : null;
    }

    private getFallbackName(): string {
        return this.itemNames()[0] ?? '';
    }

    private capitalizeName(value: string): string {
        if (!value) {
            return '';
        }
        return value.charAt(0).toUpperCase() + value.slice(1);
    }

    private getNumber(value: number | string): number {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    private cleanupAsset(): void {
        const assetId = this.selection()?.assetId;
        if (!assetId) {
            return;
        }

        this.imageUploadService.deleteAsset(assetId).subscribe({
            error: err => console.warn('Failed to delete AI product image asset', err),
        });
    }
}
