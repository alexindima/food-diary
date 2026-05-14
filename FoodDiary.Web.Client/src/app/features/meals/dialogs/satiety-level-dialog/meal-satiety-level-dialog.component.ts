import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiSatietyScaleComponent } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';

import { DEFAULT_SATIETY_LEVEL } from '../../../../shared/lib/satiety-level.utils';

export type SatietyLevelDialogData = {
    titleKey: string;
    subtitleKey?: string;
    value: number | null;
};

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
    private readonly defaultSatietyLevel = DEFAULT_SATIETY_LEVEL;

    public readonly selectedValue = signal<number | null>(null);
    public readonly subtitleKey = computed(() => this.data.subtitleKey ?? null);
    public readonly subtitle = computed(() => {
        const key = this.subtitleKey();
        return key !== null && key.trim().length > 0 ? this.translateService.instant(key) : undefined;
    });

    public constructor() {
        this.selectedValue.set(this.data.value ?? this.defaultSatietyLevel);
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
}
