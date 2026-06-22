import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiChipSelectComponent, type FdUiChipSelectOption, FdUiDateRangeInputComponent, type FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';

import { MEAL_TYPE_OPTIONS, type MealTypeOption, normalizeMealType } from '../../../../../shared/lib/meal-type.util';

export type MealListFiltersDialogData = {
    dateRange: FdUiDateRangeValue | null;
    mealTypes: string[];
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
    hasAiSession: boolean | null;
};

export type MealListFiltersDialogResult = {
    dateRange: FdUiDateRangeValue | null;
    mealTypes: string[];
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
    hasAiSession: boolean | null;
};

@Component({
    selector: 'fd-meal-list-filters-dialog',
    templateUrl: './meal-list-filters-dialog.html',
    styleUrls: ['./meal-list-filters-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiDateRangeInputComponent,
        FdUiChipSelectComponent,
        FdUiInputComponent,
        FdUiSegmentedToggleComponent,
    ],
})
export class MealListFiltersDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<MealListFiltersDialogComponent, MealListFiltersDialogResult | null>);
    private readonly data = inject<MealListFiltersDialogData>(FD_UI_DIALOG_DATA);
    private readonly translate = inject(TranslateService);

    protected readonly formModel = signal({
        dateRange: this.data.dateRange ?? null,
    });
    protected readonly form = form(this.formModel);
    protected readonly mealTypeOptions: FdUiChipSelectOption[] = MEAL_TYPE_OPTIONS.map(type => ({
        value: type,
        label: this.translate.instant(`MEAL_TYPES.${type}`),
    }));
    protected readonly selectedMealTypes = signal<string[]>(this.normalizeMealTypes(this.data.mealTypes));
    protected caloriesFromValue: number | string | null = this.data.caloriesFrom ?? null;
    protected caloriesToValue: number | string | null = this.data.caloriesTo ?? null;
    protected readonly binaryOptions: FdUiSegmentedToggleOption[] = [
        { value: 'any', label: this.translate.instant('CONSUMPTION_LIST.FILTER_ANY') },
        { value: 'yes', label: this.translate.instant('CONSUMPTION_LIST.FILTER_YES') },
        { value: 'no', label: this.translate.instant('CONSUMPTION_LIST.FILTER_NO') },
    ];
    protected imageValue: 'any' | 'yes' | 'no' = this.toBinaryValue(this.data.hasImage ?? null);
    protected aiValue: 'any' | 'yes' | 'no' = this.toBinaryValue(this.data.hasAiSession ?? null);

    protected onReset(): void {
        this.form.dateRange().value.set(null);
        this.selectedMealTypes.set([]);
        this.caloriesFromValue = null;
        this.caloriesToValue = null;
        this.imageValue = 'any';
        this.aiValue = 'any';
    }

    protected onApply(): void {
        this.dialogRef.close({
            dateRange: this.formModel().dateRange,
            mealTypes: this.selectedMealTypes(),
            caloriesFrom: this.toNumberOrNull(this.caloriesFromValue),
            caloriesTo: this.toNumberOrNull(this.caloriesToValue),
            hasImage: this.fromBinaryValue(this.imageValue),
            hasAiSession: this.fromBinaryValue(this.aiValue),
        });
    }

    protected onSubmit(event: SubmitEvent): void {
        event.preventDefault();
        this.onApply();
    }

    protected onCancel(): void {
        this.dialogRef.close(null);
    }

    protected onSelectedMealTypesChange(values: string[]): void {
        this.selectedMealTypes.set(this.normalizeMealTypes(values));
    }

    protected onImageChange(value: string): void {
        this.imageValue = this.normalizeBinaryValue(value);
    }

    protected onAiChange(value: string): void {
        this.aiValue = this.normalizeBinaryValue(value);
    }

    private toNumberOrNull(value: number | string | null): number | null {
        if (value === null || value === '') {
            return null;
        }

        const numericValue = typeof value === 'number' ? value : Number(value);
        return Number.isFinite(numericValue) && numericValue >= 0 ? numericValue : null;
    }

    private toBinaryValue(value: boolean | null): 'any' | 'yes' | 'no' {
        return value === true ? 'yes' : value === false ? 'no' : 'any';
    }

    private fromBinaryValue(value: 'any' | 'yes' | 'no'): boolean | null {
        return value === 'any' ? null : value === 'yes';
    }

    private normalizeBinaryValue(value: string): 'any' | 'yes' | 'no' {
        return value === 'yes' || value === 'no' ? value : 'any';
    }

    private normalizeMealTypes(values: readonly string[] | null | undefined): string[] {
        return [
            ...new Set((values ?? []).map(value => normalizeMealType(value)).filter((value): value is MealTypeOption => value !== null)),
        ];
    }
}
