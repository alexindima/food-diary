import { ChangeDetectionStrategy, Component, FactoryProvider, inject, input, OnInit, signal, TemplateRef, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
    TuiButton,
    tuiDialog,
    TuiDialogContext,
    TuiDialogService,
    TuiError,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective,
} from '@taiga-ui/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TUI_VALIDATION_ERRORS, TuiFieldErrorPipe } from '@taiga-ui/kit';
import { AsyncPipe } from '@angular/common';
import {
    TuiInputDateTimeModule,
    TuiInputNumberModule,
    TuiMultiSelectModule,
    TuiSelectModule,
    TuiTextfieldControllerModule,
} from '@taiga-ui/legacy';
import { ApiResponse, ErrorCode } from '../../../types/api-response.data';
import { NavigationService } from '../../../services/navigation.service';
import { FoodListDialogComponent } from '../../food-container/food-list/food-list-dialog/food-list-dialog.component';
import { TuiDay, TuiTime } from '@taiga-ui/cdk';
import { Consumption, ConsumptionManageDto, SimpleConsumption } from '../../../types/consumption.data';
import { ConsumptionService } from '../../../services/consumption.service';
import { TuiUtils } from '../../../utils/tui.utils';
import { FormGroupControls } from '../../../types/common.data';
import { nonEmptyArrayValidator } from '../../../validators/non-empty-array.validator';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: TUI_VALIDATION_ERRORS,
    useFactory: (translate: TranslateService): ValidationErrors => ({
        required: () => translate.instant('FORM_ERRORS.REQUIRED'),
        nonEmptyArray: () => translate.instant('FORM_ERRORS.NON_EMPTY_ARRAY'),
        min: ({ min }) =>
            translate.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min,
            }),
    }),
    deps: [TranslateService],
};

@Component({
    selector: 'app-base-consumption-manage',
    templateUrl: './base-consumption-manage.component.html',
    styleUrls: ['./base-consumption-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [VALIDATION_ERRORS_PROVIDER],
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiLabel,
        TuiError,
        TuiFieldErrorPipe,
        AsyncPipe,
        TuiButton,
        TuiSelectModule,
        TuiTextfieldControllerModule,
        TuiTextfieldComponent,
        TuiTextfieldDirective,
        TuiInputNumberModule,
        TuiMultiSelectModule,
        TuiInputDateTimeModule,
    ]
})
export class BaseConsumptionManageComponent implements OnInit {
    protected readonly consumptionService = inject(ConsumptionService);
    protected readonly translateService = inject(TranslateService);
    protected readonly navigationService = inject(NavigationService);
    private readonly dialogService = inject(TuiDialogService);

    private readonly dialog = tuiDialog(FoodListDialogComponent, {
        size: 'page',
        dismissible: true,
        appearance: 'without-border-radius',
    });

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<RedirectAction, void>>;

    public consumption = input<Consumption | null>();

    public globalError = signal<string | null>(null);
    public consumptionForm: FormGroup<ConsumptionFormData>;

    public constructor() {
        this.consumptionForm = new FormGroup<ConsumptionFormData>({
            date: new FormControl<[TuiDay, TuiTime]>([TuiDay.currentLocal(), TuiTime.currentLocal()], { nonNullable: true }),
            consumedFood: new FormControl<SimpleConsumption[]>([], { nonNullable: true, validators: [nonEmptyArrayValidator()] }),
            comment: new FormControl<string | null>(null),
        });

        this.consumptionForm.valueChanges.subscribe(() => this.clearGlobalError());
    }

    public ngOnInit(): void {
        const consumption = this.consumption();
        if (consumption) {
            this.populateForm(consumption);
        }
    }

    protected populateForm(consumption: Consumption): void {
        const date = new Date(consumption.date);
        const tuiDay = TuiDay.fromLocalNativeDate(date);
        const tuiTime = TuiTime.fromLocalNativeDate(date);

        const consumedFood = consumption.items.map(item => SimpleConsumption.mapFrom(item));

        this.consumptionForm.setValue({
            date: [tuiDay, tuiTime],
            consumedFood,
            comment: consumption.comment || null,
        });
    }

    public stringifyConsumptionItem(item: SimpleConsumption): string {
        return `${item.name}`;
    }

    public async openConsumptionList(): Promise<void> {
        this.dialog(null).subscribe({
            next: data => {
                this.addConsumedFood(data);
            },
        });
    }

    public addConsumedFood(food: SimpleConsumption): void {
        const consumedFoodControl = this.consumptionForm.controls.consumedFood;
        const currentValues = consumedFoodControl.value;
        consumedFoodControl?.setValue([...currentValues, food]);
    }

    public onSubmit(): void {
        this.consumptionForm.markAllAsTouched();

        if (this.consumptionForm.valid) {
            const tuiDateTime = this.consumptionForm.controls.date.value;
            const comment = this.consumptionForm.controls.comment.value;
            const consumedFood = this.consumptionForm.controls.consumedFood.value;

            const consumptionData: ConsumptionManageDto = {
                date: TuiUtils.combineTuiDayAndTuiTime(tuiDateTime[0], tuiDateTime[1]),
                comment: comment ?? undefined,
                items: consumedFood.map(simpleConsumption => SimpleConsumption.mapTo(simpleConsumption)),
            };

            const consumption = this.consumption();
            consumption ? this.updateConsumption(consumption.id, consumptionData) : this.addConsumption(consumptionData);
        }
    }

    protected async addConsumption(consumptionData: ConsumptionManageDto): Promise<void> {
        this.consumptionService.create(consumptionData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    protected async updateConsumption(id: number, consumptionData: ConsumptionManageDto): Promise<void> {
        const requestData = {
            date: consumptionData.date,
            comment: consumptionData.comment,
            items: consumptionData.items.map(item => ({
                foodId: item.foodId,
                amount: item.amount,
            })),
        };

        this.consumptionService.update(id, requestData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    private async handleSubmitResponse(response: ApiResponse<Consumption | null>): Promise<void> {
        if (response.status === 'success') {
            if (!this.consumption()) {
                this.consumptionForm.reset();
            }
            await this.showConfirmDialog();
        } else if (response.status === 'error') {
            this.handleSubmitError(response.error);
        }
    }

    private handleSubmitError(error?: ErrorCode): void {
        if (error === ErrorCode.INVALID_CREDENTIALS) {
            this.setGlobalError('FORM_ERRORS.INVALID_CREDENTIALS');
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        }
    }

    public async onMultiSelectClick(): Promise<void> {
        const consumedFoodControl = this.consumptionForm.controls.consumedFood;
        if (consumedFoodControl.value.length === 0) {
            await this.openConsumptionList();
        }
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private clearGlobalError(): void {
        this.globalError.set(null);
    }

    protected async showConfirmDialog(): Promise<void> {
        this.dialogService
            .open(this.confirmDialog, {
                dismissible: true,
                appearance: 'without-border-radius',
            })
            .subscribe(redirectAction => {
                if (redirectAction === 'Home') {
                    this.navigationService.navigateToHome();
                } else if (redirectAction === 'ConsumptionList') {
                    this.navigationService.navigateToConsumptionList();
                }
            });
    }
}

interface ValidationErrors {
    required: () => string;
    nonEmptyArray: () => string;
    min: (_params: { min: string }) => string;
}

export type RedirectAction = 'Home' | 'ConsumptionList';

interface ConsumptionFormValues {
    date: [TuiDay, TuiTime];
    consumedFood: SimpleConsumption[];
    comment: string | null;
}

export type ConsumptionFormData = FormGroupControls<ConsumptionFormValues>;
