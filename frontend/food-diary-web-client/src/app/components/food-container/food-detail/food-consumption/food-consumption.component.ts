import { ChangeDetectionStrategy, Component, FactoryProvider } from '@angular/core';
import { TuiButton, TuiError } from '@taiga-ui/core';
import { BaseFoodDetailComponent } from '../base-food-detail.component';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TUI_VALIDATION_ERRORS, TuiFieldErrorPipe } from '@taiga-ui/kit';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AsyncPipe } from '@angular/common';
import { TuiInputNumberModule } from '@taiga-ui/legacy';
import { SimpleConsumption } from '../../../../types/consumption.data';
import { FormGroupControls } from '../../../../types/common.data';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: TUI_VALIDATION_ERRORS,
    useFactory: (translate: TranslateService): ValidationErrors => ({
        required: () => translate.instant('FORM_ERRORS.REQUIRED'),
        min: ({ min }) =>
            translate.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min,
            }),
    }),
    deps: [TranslateService],
};

@Component({
    selector: 'app-food-consumption',
    templateUrl: './food-consumption.component.html',
    styleUrls: ['./food-consumption.component.less', '../base-food-detail.component.less'],
    providers: [VALIDATION_ERRORS_PROVIDER],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TuiButton,
        BaseFoodDetailComponent,
        TranslatePipe,
        AsyncPipe,
        ReactiveFormsModule,
        TuiError,
        TuiFieldErrorPipe,
        TuiInputNumberModule,
    ]
})
export class FoodConsumptionComponent extends BaseFoodDetailComponent<SimpleConsumption> {
    public consumptionForm: FormGroup<ConsumptionFormGroup>;

    public constructor() {
        super();

        this.consumptionForm = new FormGroup<FormGroupControls<ConsumptionFormValues>>({
            serving: new FormControl<number>(0, { nonNullable: true, validators: [Validators.required, Validators.min(0.001)] }),
        });
    }

    public async onAdd(): Promise<void> {
        if (!this.consumptionForm.valid) {
            return;
        }

        const result = new SimpleConsumption(this.food.id, this.food.name, this.consumptionForm.controls.serving.value);
        this.context.completeWith(result);
    }
}

interface ValidationErrors {
    required: () => string;
    min: (_params: { min: string }) => string;
}

interface ConsumptionFormValues {
    serving: number;
}

type ConsumptionFormGroup = FormGroupControls<ConsumptionFormValues>;
