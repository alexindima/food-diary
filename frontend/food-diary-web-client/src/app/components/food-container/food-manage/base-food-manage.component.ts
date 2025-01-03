import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    FactoryProvider,
    inject,
    input,
    OnInit,
    signal,
    TemplateRef,
    ViewChild,
} from '@angular/core';
import { Food, FoodManageDto, Unit } from '../../../types/food.data';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
    TuiButton,
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
import { TuiInputNumberModule, TuiSelectModule, TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { ApiResponse, ErrorCode } from '../../../types/api-response.data';
import { FoodService } from '../../../services/food.service';
import { NavigationService } from '../../../services/navigation.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroupControls } from '../../../types/common.data';

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
    selector: 'app-base-food-manage',
    templateUrl: './base-food-manage.component.html',
    styleUrls: ['./base-food-manage.component.less'],
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
    ]
})
export class BaseFoodManageComponent implements OnInit {
    protected readonly foodService = inject(FoodService);
    protected readonly translateService = inject(TranslateService);
    protected readonly navigationService = inject(NavigationService);
    private readonly dialogService = inject(TuiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<RedirectAction, void>>;

    public food = input<Food | null>();

    public globalError = signal<string | null>(null);
    public foodForm: FormGroup<FoodFormData>;
    public units = Object.values(Unit) as Unit[];

    public constructor() {
        this.foodForm = new FormGroup<FoodFormData>({
            name: new FormControl('', { nonNullable: true, validators: Validators.required }),
            barcode: new FormControl(null),
            category: new FormControl(null),
            caloriesPerBase: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0.001)] }),
            proteinsPerBase: new FormControl(0, { nonNullable: true, validators: Validators.required }),
            fatsPerBase: new FormControl(0, { nonNullable: true, validators: Validators.required }),
            carbsPerBase: new FormControl(0, { nonNullable: true, validators: Validators.required }),
            baseAmount: new FormControl(100, { nonNullable: true, validators: [Validators.required, Validators.min(0.001)] }),
            baseUnit: new FormControl(Unit.G, { nonNullable: true, validators: Validators.required }),
        });

        this.foodForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.clearGlobalError());
    }

    public ngOnInit(): void {
        const food = this.food();
        if (food) {
            this.populateForm(food);
        }
    }

    public stringifyUnits = (unit: Unit): string => {
        return this.translateService.instant(`FOOD_MANAGE.DEFAULT_SERVING_UNITS.${Unit[unit]}`);
    };

    public onSubmit(): void {
        this.foodForm.markAllAsTouched();

        if (!this.isMacronutrientsValid()) {
            this.setGlobalError('FORM_ERRORS.AT_LEAST_ONE_MACRONUTRIENT_MUST_BE_SET');
            return;
        }

        if (this.foodForm.valid) {
            const foodData = new FoodManageDto(this.foodForm.value);
            const food = this.food();
            food ? this.updateFood(food.id, foodData) : this.addFood(foodData);
        }
    }

    private isMacronutrientsValid(): boolean {
        const { proteinsPerBase, fatsPerBase, carbsPerBase } = this.foodForm.value;
        return (proteinsPerBase ?? 0) + (fatsPerBase ?? 0) + (carbsPerBase ?? 0) > 0;
    }

    protected populateForm(food: Food): void {
        this.foodForm.patchValue(food);
    }

    protected async addFood(foodData: FoodManageDto): Promise<void> {
        this.foodService.create(foodData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    protected async updateFood(id: number, foodData: FoodManageDto): Promise<void> {
        this.foodService.update(id, foodData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    private async handleSubmitResponse(response: ApiResponse<Food | null>): Promise<void> {
        if (response.status === 'success') {
            if (!this.food()) {
                this.foodForm.reset();
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
                } else if (redirectAction === 'FoodList') {
                    this.navigationService.navigateToFoodList();
                }
            });
    }

    protected readonly Unit = Unit;
}

export interface FoodFormValues {
    name: string;
    barcode: string | null;
    category: string | null;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    baseAmount: number;
    baseUnit: Unit;
}

export type FoodFormData = FormGroupControls<FoodFormValues>;

interface ValidationErrors {
    required: () => string;
    min: (_params: { min: string }) => string;
}

export type RedirectAction = 'Home' | 'FoodList';
