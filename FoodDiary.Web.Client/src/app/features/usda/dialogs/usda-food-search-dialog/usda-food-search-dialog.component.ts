import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';

import { UsdaService } from '../../api/usda.service';
import type { UsdaFood } from '../../models/usda.data';

@Component({
    selector: 'fd-usda-food-search-dialog',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiInputComponent,
    ],
    templateUrl: './usda-food-search-dialog.component.html',
    styleUrls: ['./usda-food-search-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsdaFoodSearchDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<UsdaFoodSearchDialogComponent, UsdaFood | null>);
    private readonly usdaService = inject(UsdaService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly searchQuery = signal('');
    public readonly results = signal<UsdaFood[]>([]);
    public readonly isLoading = signal(false);
    public readonly selectedFood = signal<UsdaFood | null>(null);

    public constructor() {
        toObservable(this.searchQuery)
            .pipe(
                debounceTime(300),
                distinctUntilChanged(),
                switchMap(query => {
                    if (query.length < 2) {
                        this.results.set([]);
                        this.isLoading.set(false);
                        return of<UsdaFood[]>([]);
                    }
                    this.isLoading.set(true);
                    return this.usdaService.searchFoods(query);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(foods => {
                this.results.set(foods);
                this.isLoading.set(false);
            });
    }

    public onSearchChange(value: string): void {
        this.searchQuery.set(value);
        this.selectedFood.set(null);
    }

    public selectFood(food: UsdaFood): void {
        this.selectedFood.set(food);
    }

    public onConfirm(): void {
        this.dialogRef.close(this.selectedFood());
    }

    public onCancel(): void {
        this.dialogRef.close(null);
    }
}
