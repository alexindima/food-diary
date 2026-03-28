import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { NutrientBadgesComponent } from '../nutrient-badges/nutrient-badges.component';

export interface RecipeCardStep {
    ingredients?: Array<unknown> | null;
}

export interface RecipeCardItem {
    name: string;
    imageUrl?: string | null;
    isOwnedByCurrentUser: boolean;
    prepTime?: number | null;
    cookTime?: number | null;
    totalProteins?: number | null;
    totalFats?: number | null;
    totalCarbs?: number | null;
    totalFiber?: number | null;
    totalAlcohol?: number | null;
    totalCalories?: number | null;
    steps?: RecipeCardStep[] | null;
}

@Component({
    selector: 'fd-recipe-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiIconModule, FdUiButtonComponent, NutrientBadgesComponent],
    templateUrl: './recipe-card.component.html',
    styleUrl: './recipe-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCardComponent {
    @Input({ required: true }) public recipe!: RecipeCardItem;
    @Input() public imageUrl?: string;
    @Output() public open = new EventEmitter<void>();
    @Output() public addToMeal = new EventEmitter<void>();

    public handleOpen(): void {
        this.open.emit();
    }

    public handleAdd(event: Event): void {
        event.stopPropagation();
        this.addToMeal.emit();
    }

    public getTotalTime(): number | null {
        const prep = this.recipe?.prepTime ?? 0;
        const cook = this.recipe?.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    }

    public getIngredientCount(): number {
        if (!this.recipe?.steps?.length) {
            return 0;
        }

        return this.recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    }
}
