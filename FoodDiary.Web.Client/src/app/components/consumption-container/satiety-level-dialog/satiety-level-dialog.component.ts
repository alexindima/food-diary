import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiSatietyScaleComponent } from '../../../ui-kit/satiety-scale/fd-ui-satiety-scale.component';
import { FdUiButtonComponent } from '../../../ui-kit/button/fd-ui-button.component';
import { FormsModule } from '@angular/forms';
import { FdUiDialogComponent } from '../../../ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from '../../../ui-kit/dialog/fd-ui-dialog-footer.directive';

export interface SatietyLevelDialogData {
    titleKey: string;
    subtitleKey?: string;
    value: number | null;
}

@Component({
    selector: 'fd-satiety-level-dialog',
    standalone: true,
    templateUrl: './satiety-level-dialog.component.html',
    imports: [
        CommonModule,
        FormsModule,
        TranslateModule,
        FdUiSatietyScaleComponent,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SatietyLevelDialogComponent {
    public selectedValue = signal<number | null>(null);

    public constructor(
        @Inject(MAT_DIALOG_DATA) public readonly data: SatietyLevelDialogData,
        private readonly dialogRef: MatDialogRef<SatietyLevelDialogComponent>,
        private readonly translateService: TranslateService,
    ) {
        this.selectedValue.set(this.data.value ?? null);
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
        return `${value} — ${title}`;
    }
}
