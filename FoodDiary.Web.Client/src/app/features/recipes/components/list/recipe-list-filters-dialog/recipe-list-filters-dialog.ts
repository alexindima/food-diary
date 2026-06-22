import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
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
        FdUiInputComponent,
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
    protected readonly imageOptions: FdUiSegmentedToggleOption[] = [
        { value: 'any', label: this.translate.instant('RECIPE_LIST.FILTER_IMAGE_ANY') },
        { value: 'with', label: this.translate.instant('RECIPE_LIST.FILTER_IMAGE_WITH') },
        { value: 'without', label: this.translate.instant('RECIPE_LIST.FILTER_IMAGE_WITHOUT') },
    ];

    protected visibilityValue: 'all' | 'mine' = this.data.onlyMine ? 'mine' : 'all';
    protected categoryValue: string | number | null = this.data.category;
    protected maxTotalTimeValue: string | number | null = this.data.maxTotalTime;
    protected caloriesFromValue: string | number | null = this.data.caloriesFrom;
    protected caloriesToValue: string | number | null = this.data.caloriesTo;
    protected imageValue: 'any' | 'with' | 'without' =
        this.data.hasImage === true ? 'with' : this.data.hasImage === false ? 'without' : 'any';

    protected onVisibilityChange(value: string): void {
        this.visibilityValue = value === 'mine' ? 'mine' : 'all';
    }

    protected onImageChange(value: string): void {
        this.imageValue = value === 'with' || value === 'without' ? value : 'any';
    }

    protected onApply(): void {
        this.dialogRef.close({
            onlyMine: this.visibilityValue === 'mine',
            category: this.toTextOrNull(this.categoryValue),
            maxTotalTime: this.toNumberOrNull(this.maxTotalTimeValue),
            caloriesFrom: this.toNumberOrNull(this.caloriesFromValue),
            caloriesTo: this.toNumberOrNull(this.caloriesToValue),
            hasImage: this.imageValue === 'any' ? null : this.imageValue === 'with',
        });
    }

    protected onCancel(): void {
        this.dialogRef.close(null);
    }

    private toTextOrNull(value: string | number | null): string | null {
        const text = value === null ? '' : String(value).trim();
        return text.length > 0 ? text : null;
    }

    private toNumberOrNull(value: string | number | null): number | null {
        if (value === null || value === '') {
            return null;
        }

        const numericValue = typeof value === 'number' ? value : Number(value);
        return Number.isFinite(numericValue) && numericValue >= 0 ? numericValue : null;
    }
}
