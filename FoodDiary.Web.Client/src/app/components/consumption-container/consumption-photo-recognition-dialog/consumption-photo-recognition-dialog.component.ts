import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { ImageUploadFieldComponent } from '../../shared/image-upload-field/image-upload-field.component';
import { ImageSelection } from '../../../types/image-upload.data';
import { AiFoodService } from '../../../services/ai-food.service';
import { FoodNutritionResponse, FoodVisionItem } from '../../../types/ai.data';
import { FdUiDialogRef, FdUiIconModule } from 'fd-ui-kit/material';
import { catchError, of } from 'rxjs';

@Component({
    selector: 'fd-consumption-photo-recognition-dialog',
    standalone: true,
    templateUrl: './consumption-photo-recognition-dialog.component.html',
    styleUrls: ['./consumption-photo-recognition-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiIconModule,
        ImageUploadFieldComponent,
    ],
})
export class ConsumptionPhotoRecognitionDialogComponent {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly translateService = inject(TranslateService);
    private readonly dialogRef = inject(
        FdUiDialogRef<ConsumptionPhotoRecognitionDialogComponent, FoodVisionItem[] | null>,
        { optional: true },
    );

    public readonly isLoading = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly results = signal<FoodVisionItem[]>([]);
    public readonly selection = signal<ImageSelection | null>(null);
    public readonly hasAnalyzed = signal(false);
    public readonly isNutritionLoading = signal(false);
    public readonly nutrition = signal<FoodNutritionResponse | null>(null);
    public readonly nutritionErrorKey = signal<string | null>(null);
    public readonly statusKey = computed(() => {
        if (!this.selection()) {
            return null;
        }

        if (this.isLoading()) {
            return 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.STATUS_ANALYZING';
        }

        if (this.hasAnalyzed()) {
            return 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.STATUS_DONE';
        }

        return null;
    });

    public getDisplayName(item: FoodVisionItem): string {
        const rawName = item.nameLocal?.trim() || item.nameEn;
        return this.capitalizeLabel(rawName);
    }

    public formatAmount(item: FoodVisionItem): string {
        const amount = item.amount ?? '';
        const unitKey = this.resolveUnitKey(item.unit);
        const unitLabel = unitKey ? this.translateService.instant(unitKey) : item.unit;
        return unitLabel ? `${amount} ${unitLabel}`.trim() : `${amount}`.trim();
    }

    private resolveUnitKey(unit?: string | null): string | null {
        if (!unit) {
            return null;
        }

        const normalized = unit.trim().toLowerCase();
        const unitMap: Record<string, string> = {
            g: 'GENERAL.UNITS.G',
            gram: 'GENERAL.UNITS.G',
            grams: 'GENERAL.UNITS.G',
            gr: 'GENERAL.UNITS.G',
            ml: 'GENERAL.UNITS.ML',
            l: 'GENERAL.UNITS.ML',
            pcs: 'GENERAL.UNITS.PCS',
            pc: 'GENERAL.UNITS.PCS',
            piece: 'GENERAL.UNITS.PCS',
            pieces: 'GENERAL.UNITS.PCS',
            kcal: 'GENERAL.UNITS.KCAL',
        };

        return unitMap[normalized] ?? null;
    }

    private capitalizeLabel(value?: string | null): string {
        if (!value) {
            return '';
        }

        const trimmed = value.trim();
        if (!trimmed) {
            return '';
        }

        const [first, ...rest] = trimmed;
        return `${first.toLocaleUpperCase()}${rest.join('')}`;
    }

    public onImageChanged(selection: ImageSelection | null): void {
        this.selection.set(selection);
        this.errorKey.set(null);
        this.results.set([]);
        this.hasAnalyzed.set(false);
        this.isNutritionLoading.set(false);
        this.nutrition.set(null);
        this.nutritionErrorKey.set(null);

        if (!selection?.assetId) {
            return;
        }

        this.runAnalysis(selection.assetId);
    }

    public onReanalyze(): void {
        const assetId = this.selection()?.assetId;
        if (!assetId || this.isLoading()) {
            return;
        }

        this.errorKey.set(null);
        this.results.set([]);
        this.hasAnalyzed.set(false);
        this.isNutritionLoading.set(false);
        this.nutrition.set(null);
        this.nutritionErrorKey.set(null);
        this.runAnalysis(assetId);
    }

    public addToMeal(): void {
        return;
    }

    public close(): void {
        this.dialogRef?.close(null);
    }

    private runAnalysis(assetId: string): void {
        this.isLoading.set(true);
        this.aiFoodService
            .analyzeFoodImage({ imageAssetId: assetId })
            .pipe(
                catchError(err => {
                    if (err?.status === 403) {
                        this.errorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM');
                    } else {
                        this.errorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_GENERIC');
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
        this.nutrition.set(null);
        this.nutritionErrorKey.set(null);

        this.aiFoodService
            .calculateNutrition({ items })
            .pipe(
                catchError(err => {
                    console.error('Failed to calculate nutrition', err);
                    this.nutritionErrorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR');
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.isNutritionLoading.set(false);
                if (!response) {
                    return;
                }
                this.nutrition.set(response);
            });
    }

    public formatMacro(value: number, unitKey: string): string {
        const locale = this.translateService.currentLang || this.translateService.defaultLang || 'en';
        const hasFraction = Math.abs(value % 1) > 0.01;
        const formatter = new Intl.NumberFormat(locale, {
            maximumFractionDigits: hasFraction ? 1 : 0,
            minimumFractionDigits: hasFraction ? 1 : 0,
        });
        const unitLabel = this.translateService.instant(unitKey);
        return `${formatter.format(value)} ${unitLabel}`.trim();
    }
}
