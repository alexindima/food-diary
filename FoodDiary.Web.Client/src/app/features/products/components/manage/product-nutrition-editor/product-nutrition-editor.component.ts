import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';

import type {
    NutritionMacroState,
    NutritionMismatchWarning,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import {
    type NutritionControlNames,
    NutritionEditorComponent,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import type { NutritionMode, ProductFormData } from '../base-product-manage.types';

@Component({
    selector: 'fd-product-nutrition-editor',
    standalone: true,
    templateUrl: './product-nutrition-editor.component.html',
    styleUrls: ['./product-nutrition-editor.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiSegmentedToggleComponent,
        NutritionEditorComponent,
    ],
})
export class ProductNutritionEditorComponent {
    public readonly formGroup = input.required<FormGroup<ProductFormData>>();
    public readonly nutritionMode = input.required<NutritionMode>();
    public readonly nutritionModeOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly macroBarState = input.required<NutritionMacroState>();
    public readonly nutritionWarning = input.required<NutritionMismatchWarning | null>();
    public readonly caloriesError = input.required<string | null>();
    public readonly macrosError = input.required<string | null>();
    public readonly nutritionControlNames = input.required<NutritionControlNames>();

    public readonly nutritionModeChange = output<string>();
    public readonly openAiRecognition = output();
}
