import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';

import type { RecipeListFiltersDialogData, RecipeListFiltersDialogResult } from './recipe-list-filters-dialog.types';

@Component({
    selector: 'fd-recipe-list-filters-dialog',
    templateUrl: './recipe-list-filters-dialog.html',
    styleUrls: ['./recipe-list-filters-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
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

    protected readonly visibilityOptions: FdUiSegmentedToggleOption[] = [
        { value: 'all', label: this.translate.instant('RECIPE_LIST.FILTER_ALL_RECIPES') },
        { value: 'mine', label: this.translate.instant('RECIPE_LIST.FILTER_MY_RECIPES') },
    ];

    protected visibilityValue: 'all' | 'mine' = this.data.onlyMine ? 'mine' : 'all';

    protected onVisibilityChange(value: string): void {
        this.visibilityValue = value === 'mine' ? 'mine' : 'all';
    }

    protected onApply(): void {
        this.dialogRef.close({ onlyMine: this.visibilityValue === 'mine' });
    }

    protected onCancel(): void {
        this.dialogRef.close(null);
    }
}
