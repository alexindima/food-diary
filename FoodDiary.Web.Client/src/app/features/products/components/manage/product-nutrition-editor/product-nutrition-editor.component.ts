import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiSegmentedToggleComponent, FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import {
    NutritionEditorComponent,
    NutritionControlNames,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import {
    NutritionMacroState,
    NutritionMismatchWarning,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { ProductFormData, NutritionMode } from '../base-product-manage.component';

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
    public formGroup = input.required<FormGroup<ProductFormData>>();
    public nutritionMode = input.required<NutritionMode>();
    public nutritionModeOptions = input.required<FdUiSegmentedToggleOption[]>();
    public macroBarState = input.required<NutritionMacroState>();
    public nutritionWarning = input.required<NutritionMismatchWarning | null>();
    public caloriesError = input.required<string | null>();
    public macrosError = input.required<string | null>();
    public nutritionControlNames = input.required<NutritionControlNames>();

    public nutritionModeChange = output<string>();
    public openAiRecognition = output<void>();
}
