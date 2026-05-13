import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { EMPTY, type Observable } from 'rxjs';

import type { FoodNutritionResponse } from '../../../../../shared/models/ai.data';
import { MeasurementUnit } from '../../../models/product.data';
import type { ProductAiRecognitionFormGroup } from '../product-ai-recognition-dialog.types';

@Component({
    selector: 'fd-product-ai-recognition-result',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiInputComponent, FdUiSelectComponent],
    templateUrl: './product-ai-recognition-result.component.html',
    styleUrl: '../product-ai-recognition-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductAiRecognitionResultComponent {
    private readonly translateService = inject(TranslateService);

    public readonly form = input.required<ProductAiRecognitionFormGroup>();
    public readonly nutrition = input.required<FoodNutritionResponse>();
    public readonly itemNames = input.required<readonly string[]>();
    public readonly hasMultipleItems = computed(() => this.itemNames().length > 1);
    public readonly unitOptions = signal<Array<FdUiSelectOption<MeasurementUnit>>>([]);

    public constructor() {
        effect(onCleanup => {
            const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
            const refresh = (): void => {
                this.buildUnitOptions();
            };

            refresh();
            const subscription = languageChanges.subscribe(() => {
                refresh();
            });
            onCleanup(() => {
                subscription.unsubscribe();
            });
        });
    }

    private buildUnitOptions(): void {
        this.unitOptions.set(
            (Object.values(MeasurementUnit) as MeasurementUnit[]).map(unit => ({
                value: unit,
                label: this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${MeasurementUnit[unit]}`),
            })),
        );
    }
}
