import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { fdUiCoerceInputTextValue, FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';

import { UsdaFoodSearchFacade } from '../../lib/usda-food-search.facade';
import type { UsdaFood } from '../../models/usda.data';

@Component({
    selector: 'fd-usda-food-search-dialog',
    imports: [TranslatePipe, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './usda-food-search-dialog.html',
    styleUrls: ['./usda-food-search-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsdaFoodSearchDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<UsdaFoodSearchDialogComponent, UsdaFood | null>);
    private readonly usdaFoodSearchFacade = inject(UsdaFoodSearchFacade);

    protected readonly searchQuery = this.usdaFoodSearchFacade.searchQuery;
    protected readonly results = this.usdaFoodSearchFacade.results;
    protected readonly isLoading = this.usdaFoodSearchFacade.isLoading;
    protected readonly selectedFood = this.usdaFoodSearchFacade.selectedFood;

    public constructor() {
        this.usdaFoodSearchFacade.reset();
    }

    protected onSearchChange(value: string): void {
        this.usdaFoodSearchFacade.updateSearchQuery(value);
    }

    protected onSearchControlChange(value: FdUiInputValue): void {
        this.onSearchChange(fdUiCoerceInputTextValue(value));
    }

    protected selectFood(food: UsdaFood): void {
        this.usdaFoodSearchFacade.selectFood(food);
    }

    protected onConfirm(): void {
        this.dialogRef.close(this.selectedFood());
    }

    protected onCancel(): void {
        this.dialogRef.close(null);
    }
}
