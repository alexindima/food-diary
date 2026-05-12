import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDateRangeInputComponent, type FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
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
    templateUrl: './meal-list-filters-dialog.component.html',
    styleUrls: ['./meal-list-filters-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
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

    public readonly dateRangeControl = new FormControl<FdUiDateRangeValue | null>(this.data.dateRange ?? null);

    public onReset(): void {
        this.dateRangeControl.setValue(null);
    }

    public onApply(): void {
        this.dialogRef.close({
            dateRange: this.dateRangeControl.value,
        });
    }

    public onCancel(): void {
        this.dialogRef.close(null);
    }
}
