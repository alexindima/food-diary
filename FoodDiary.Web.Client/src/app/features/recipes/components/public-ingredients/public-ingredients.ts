import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, model, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective } from 'fd-ui-kit';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import type { ShoppingListItemDto } from '../../../shopping-lists/models/shopping-list.data';
import { scaleIngredient } from '../../lib/public-recipe.utils';
import type { PublicRecipe, PublicRecipeIngredient } from '../../models/public-recipe.data';

const DEFAULT_MAX_SERVINGS = 50;

@Component({
    selector: 'fd-public-ingredients',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './public-ingredients.html',
    styleUrl: './public-ingredients.scss',
    imports: [DecimalPipe, RouterLink, TranslatePipe, FdUiButtonComponent, FdUiHintDirective],
})
export class PublicIngredientsComponent {
    private readonly toast = inject(FdUiToastService);
    private readonly translate = inject(TranslateService);
    public readonly recipe = input.required<PublicRecipe>();
    public readonly servings = model.required<number>();
    public readonly shoppingBusy = input(false);
    public readonly shoppingTarget = input<string | null>(null);
    public readonly shoppingMessage = input<string | null>(null);
    public readonly addedIngredients = input<ReadonlySet<number>>(new Set());
    public readonly shoppingAdd = output<{ index: number; item: ShoppingListItemDto }>();
    public readonly shoppingAddAll = output<Array<{ index: number; item: ShoppingListItemDto }>>();
    public readonly shoppingChange = output();
    protected readonly remaining = computed(() =>
        this.ingredients()
            .map((ingredient, index) => ({ ingredient, index }))
            .filter(
                ({ ingredient, index }) =>
                    ingredient.isAvailable && ingredient.name !== null && ingredient.name.length > 0 && !this.addedIngredients().has(index),
            ),
    );
    protected addAll(): void {
        this.shoppingAddAll.emit(this.remaining().map(({ ingredient, index }) => ({ index, item: this.shoppingItem(ingredient) })));
    }
    public constructor() {
        effect(() => {
            if (this.shoppingMessage() === 'PUBLIC_RECIPES.SHOPPING_ERROR') {
                this.toast.error(this.translate.instant('PUBLIC_RECIPES.SHOPPING_ERROR'));
            }
        });
    }
    protected add(index: number, ingredient: PublicRecipeIngredient): void {
        if (!ingredient.isAvailable || ingredient.name === null || ingredient.name.length === 0) {
            return;
        }
        this.shoppingAdd.emit({ index, item: this.shoppingItem(ingredient) });
    }
    private shoppingItem(ingredient: PublicRecipeIngredient): ShoppingListItemDto {
        const units = new Map([
            ['g', 'G'],
            ['gram', 'G'],
            ['ml', 'Ml'],
            ['milliliter', 'Ml'],
            ['pcs', 'Pcs'],
            ['piece', 'Pcs'],
        ]);
        const unit = units.get(ingredient.unit?.toLowerCase() ?? '') ?? null;
        return {
            name: ingredient.name,
            amount: ingredient.amountText !== null ? null : this.amount(ingredient),
            unit: ingredient.amountText !== null ? null : unit,
            note: this.shoppingNote(ingredient, unit),
            isChecked: false,
        };
    }
    private shoppingNote(ingredient: PublicRecipeIngredient, unit: string | null): string | null {
        if (ingredient.amountText !== null) {
            return ingredient.amountText;
        }
        if (unit !== null || ingredient.unit === null) {
            return null;
        }
        const key = this.unitKey(ingredient.unit);
        return this.translate.instant(key.length > 0 ? key : ingredient.unit);
    }
    protected readonly maxServings = computed(() => Math.max(DEFAULT_MAX_SERVINGS, this.recipe().servings));
    protected readonly servingsWidth = computed(() => `${String(this.maxServings()).length}ch`);
    protected readonly ingredients = computed(() => this.recipe().steps.flatMap(step => step.ingredients));
    protected amount(ingredient: PublicRecipeIngredient): number | null {
        return scaleIngredient(ingredient, this.servings(), this.recipe().servings);
    }
    protected unitKey(unit: string | null): string {
        const keys = new Map([
            ['g', 'GRAMS'],
            ['Gram', 'GRAMS'],
            ['G', 'GRAMS'],
            ['ml', 'ML'],
            ['Milliliter', 'ML'],
            ['Ml', 'ML'],
            ['ML', 'ML'],
            ['serving', 'SERVINGS'],
            ['Piece', 'PIECES'],
            ['piece', 'PIECES'],
            ['PCS', 'PIECES'],
        ]);
        const key = keys.get(unit ?? '');
        return key === undefined ? '' : `PUBLIC_RECIPES.${key}`;
    }
    protected changeServings(delta: number): void {
        this.servings.set(Math.min(this.maxServings(), Math.max(1, this.servings() + delta)));
    }
}
