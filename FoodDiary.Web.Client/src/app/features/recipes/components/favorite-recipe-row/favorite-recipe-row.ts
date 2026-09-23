import { ChangeDetectionStrategy, Component, computed, ElementRef, input, output, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import { NutrientBadgesComponent } from '../../../../components/shared/nutrient-badges/nutrient-badges';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { FavoriteRecipe } from '../../models/recipe.data';

@Component({
    selector: 'fd-favorite-recipe-row',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent, NutrientBadgesComponent, LocalizedNumberPipe],
    templateUrl: './favorite-recipe-row.html',
    styleUrl: './favorite-recipe-row.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FavoriteRecipeRowComponent {
    public readonly recipe = input.required<FavoriteRecipe>();
    public readonly remove = output<FavoriteRecipe>();
    public readonly removed = input(false);
    public readonly restoring = input(false);
    public readonly restoreFailed = input(false);
    public readonly restore = output<FavoriteRecipe>();
    protected readonly undoMessageKey = computed(() =>
        this.restoreFailed() ? 'RECIPE_FAVORITES.RESTORE_ERROR' : 'RECIPE_FAVORITES.REMOVED',
    );
    public readonly action = viewChild<FdUiButtonComponent, ElementRef<HTMLElement>>('action', { read: ElementRef });

    public readonly removing = input(false);
    public readonly loading = input(false);
    public readonly disabled = input(false);
    public readonly add = output<FavoriteRecipe>();
    protected readonly locale = injectCurrentLanguage();
    protected readonly name = computed(() => {
        const name = this.recipe().name?.trim();
        return name !== undefined && name.length > 0 ? name : this.recipe().recipeName;
    });
    protected readonly composition = computed(() => this.recipe().ingredientNames?.join(', ') ?? '');
    private readonly failedImages = signal<ReadonlySet<string>>(new Set());
    protected readonly images = computed(() => {
        const cover = this.recipe().imageUrl?.trim();
        const failed = this.failedImages();
        if (cover !== undefined && cover.length > 0 && !failed.has(cover)) {
            return [cover];
        }
        return [];
    });
    protected imageFailed(url: string): void {
        this.failedImages.update(current => new Set([...current, url]));
    }
}
