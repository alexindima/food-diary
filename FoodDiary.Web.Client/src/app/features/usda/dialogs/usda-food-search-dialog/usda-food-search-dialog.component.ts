import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, Subject, switchMap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { UsdaService } from '../../api/usda.service';
import { UsdaFood } from '../../models/usda.data';

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
    template: `
        <fd-ui-dialog [title]="'USDA_SEARCH.TITLE' | translate">
            <fd-ui-input
                [placeholder]="'USDA_SEARCH.PLACEHOLDER' | translate"
                [ngModel]="searchQuery()"
                (ngModelChange)="onSearchChange($event)"
            />

            <div class="results-container">
                @if (isLoading()) {
                    <div class="loading">{{ 'COMMON.LOADING' | translate }}...</div>
                }
                @if (!isLoading() && results().length === 0 && searchQuery().length > 0) {
                    <div class="no-results">{{ 'USDA_SEARCH.NO_RESULTS' | translate }}</div>
                }
                @for (food of results(); track food.fdcId) {
                    <button class="result-item" [class.selected]="selectedFood()?.fdcId === food.fdcId" (click)="selectFood(food)">
                        <span class="food-name">{{ food.description }}</span>
                        @if (food.foodCategory) {
                            <span class="food-category">{{ food.foodCategory }}</span>
                        }
                    </button>
                }
            </div>

            <div *fdUiDialogFooter class="dialog-footer">
                <fd-ui-button variant="text" (click)="onCancel()">
                    {{ 'COMMON.CANCEL' | translate }}
                </fd-ui-button>
                <fd-ui-button variant="flat" [disabled]="!selectedFood()" (click)="onConfirm()">
                    {{ 'USDA_SEARCH.LINK' | translate }}
                </fd-ui-button>
            </div>
        </fd-ui-dialog>
    `,
    styles: [
        `
            .results-container {
                max-height: 320px;
                overflow-y: auto;
                margin-top: 12px;
            }

            .result-item {
                display: flex;
                flex-direction: column;
                align-items: flex-start;
                width: 100%;
                padding: 10px 12px;
                border: none;
                background: none;
                cursor: pointer;
                text-align: left;
                border-radius: 6px;
                transition: background-color 0.15s;

                &:hover {
                    background-color: var(--fd-surface-hover, rgba(0, 0, 0, 0.04));
                }

                &.selected {
                    background-color: var(--fd-primary-light, rgba(25, 118, 210, 0.08));
                }
            }

            .food-name {
                font-size: 14px;
                font-weight: 500;
            }

            .food-category {
                font-size: 12px;
                color: var(--fd-text-secondary, #666);
                margin-top: 2px;
            }

            .loading,
            .no-results {
                padding: 24px;
                text-align: center;
                color: var(--fd-text-secondary, #666);
            }

            .dialog-footer {
                display: flex;
                justify-content: flex-end;
                gap: 8px;
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsdaFoodSearchDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<UsdaFoodSearchDialogComponent, UsdaFood | null>);
    private readonly usdaService = inject(UsdaService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly searchSubject = new Subject<string>();

    public readonly searchQuery = signal('');
    public readonly results = signal<UsdaFood[]>([]);
    public readonly isLoading = signal(false);
    public readonly selectedFood = signal<UsdaFood | null>(null);

    public constructor() {
        this.searchSubject
            .pipe(
                debounceTime(300),
                distinctUntilChanged(),
                switchMap(query => {
                    if (query.length < 2) {
                        this.results.set([]);
                        this.isLoading.set(false);
                        return [];
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
        this.searchSubject.next(value);
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
