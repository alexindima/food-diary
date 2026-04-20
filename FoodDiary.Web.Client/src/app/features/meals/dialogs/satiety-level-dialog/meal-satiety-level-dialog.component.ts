import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiSatietyScaleComponent } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';

export interface SatietyLevelDialogData {
    titleKey: string;
    subtitleKey?: string;
    value: number | null;
}

@Component({
    selector: 'fd-meal-satiety-level-dialog',
    standalone: true,
    templateUrl: './meal-satiety-level-dialog.component.html',
    styleUrls: ['./meal-satiety-level-dialog.component.scss'],
    imports: [FormsModule, TranslateModule, FdUiSatietyScaleComponent, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealSatietyLevelDialogComponent {
    public readonly data = inject<SatietyLevelDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<MealSatietyLevelDialogComponent>>(FdUiDialogRef);
    private readonly translateService = inject(TranslateService);

    public selectedValue = signal<number | null>(null);

    public constructor() {
        this.selectedValue.set(this.data.value ?? 0);
    }

    public onValueSelected(level: number): void {
        this.selectedValue.set(level);
    }

    public closeWithValue(): void {
        this.dialogRef.close(this.selectedValue());
    }

    public close(): void {
        this.dialogRef.close();
    }

    public getSelectionLabel(): string {
        const value = this.selectedValue();
        if (!value) {
            return this.translateService.instant('CONSUMPTION_MANAGE.SATIETY_NOT_SELECTED');
        }

        const title = this.translateService.instant(`HUNGER_SCALE.LEVEL_${value}.TITLE`);
        return `${value} - ${title}`;
    }
}
