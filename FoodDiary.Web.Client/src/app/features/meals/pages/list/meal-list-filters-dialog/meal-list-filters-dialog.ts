import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDateRangeInputComponent, type FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

export type MealListFiltersDialogData = {
    dateRange: FdUiDateRangeValue | null;
};

export type MealListFiltersDialogResult = {
    dateRange: FdUiDateRangeValue | null;
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
    ],
})
export class MealListFiltersDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<MealListFiltersDialogComponent, MealListFiltersDialogResult | null>);
    private readonly data = inject<MealListFiltersDialogData>(FD_UI_DIALOG_DATA);

    protected readonly formModel = signal({
        dateRange: this.data.dateRange ?? null,
    });
    private readonly submitFiltersFormAsync = async (): Promise<void> => {
        this.onApply();
        await Promise.resolve();
    };
    protected readonly form = form(this.formModel, {
        submission: {
            action: this.submitFiltersFormAsync,
        },
    });

    protected onReset(): void {
        this.form.dateRange().value.set(null);
    }

    protected onApply(): void {
        this.dialogRef.close({
            dateRange: this.formModel().dateRange,
        });
    }

    protected onCancel(): void {
        this.dialogRef.close(null);
    }
}
