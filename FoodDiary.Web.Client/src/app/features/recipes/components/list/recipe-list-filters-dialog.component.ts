import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiSegmentedToggleComponent, FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';

export interface RecipeListFiltersDialogData {
    onlyMine: boolean;
}

export interface RecipeListFiltersDialogResult {
    onlyMine: boolean;
}

@Component({
    selector: 'fd-recipe-list-filters-dialog',
    templateUrl: './recipe-list-filters-dialog.component.html',
    styleUrls: ['./recipe-list-filters-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiSegmentedToggleComponent,
    ],
})
export class RecipeListFiltersDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<RecipeListFiltersDialogComponent, RecipeListFiltersDialogResult | null>);
    private readonly translate = inject(TranslateService);
    private readonly data = inject<RecipeListFiltersDialogData>(FD_UI_DIALOG_DATA);

    public readonly visibilityOptions: FdUiSegmentedToggleOption[] = [
        { value: 'all', label: this.translate.instant('RECIPE_LIST.FILTER_ALL_RECIPES') },
        { value: 'mine', label: this.translate.instant('RECIPE_LIST.FILTER_MY_RECIPES') },
    ];

    public visibilityValue: 'all' | 'mine' = this.data.onlyMine ? 'mine' : 'all';

    public onVisibilityChange(value: string): void {
        this.visibilityValue = value === 'mine' ? 'mine' : 'all';
    }

    public onApply(): void {
        this.dialogRef.close({ onlyMine: this.visibilityValue === 'mine' });
    }

    public onCancel(): void {
        this.dialogRef.close(null);
    }
}
