import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { UsdaFoodSearchFacade } from '../../lib/usda-food-search.facade';
import type { UsdaFood } from '../../models/usda.data';

@Component({
    selector: 'fd-usda-food-search-dialog',
    imports: [FormsModule, TranslatePipe, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './usda-food-search-dialog.component.html',
    styleUrls: ['./usda-food-search-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsdaFoodSearchDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<UsdaFoodSearchDialogComponent, UsdaFood | null>);
    private readonly usdaFoodSearchFacade = inject(UsdaFoodSearchFacade);

    public readonly searchQuery = this.usdaFoodSearchFacade.searchQuery;
    public readonly results = this.usdaFoodSearchFacade.results;
    public readonly isLoading = this.usdaFoodSearchFacade.isLoading;
    public readonly selectedFood = this.usdaFoodSearchFacade.selectedFood;

    public constructor() {
        this.usdaFoodSearchFacade.reset();
    }

    public onSearchChange(value: string): void {
        this.usdaFoodSearchFacade.updateSearchQuery(value);
    }

    public selectFood(food: UsdaFood): void {
        this.usdaFoodSearchFacade.selectFood(food);
    }

    public onConfirm(): void {
        this.dialogRef.close(this.selectedFood());
    }

    public onCancel(): void {
        this.dialogRef.close(null);
    }
}
