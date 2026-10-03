import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';
import { firstValueFrom, map } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { AuthService } from '../../../../services/auth.service';
import { recipeCategoryKey } from '../../../../shared/models/recipe-category';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { PublicAuthDialogService } from '../../../public/contracts/auth-dialog';
import { ShoppingListAddFacade } from '../../../shopping-lists/contracts/shopping-list-add';
import { PublicRecipeGalleryComponent } from '../../components/public-gallery/public-gallery';
import { PublicIngredientsComponent } from '../../components/public-ingredients/public-ingredients';
import { PublicRecipeNavigationComponent } from '../../components/public-navigation/public-navigation';
import { PublicNutritionComponent } from '../../components/public-nutrition/public-nutrition';
import { PublicStepsComponent } from '../../components/public-steps/public-steps';
import { recipeImages } from '../../lib/public-recipe.utils';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import type { PublicRecipePageData } from '../../resolvers/public-recipe.resolver';

@Component({
    selector: 'fd-public-recipe-detail',
    templateUrl: './public-detail.html',
    styleUrl: './public-detail.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [PublicRecipesFacade, ShoppingListAddFacade],
    imports: [
        PageBodyComponent,
        PublicNutritionComponent,
        PublicIngredientsComponent,
        PublicStepsComponent,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        PublicRecipeGalleryComponent,
        PublicRecipeNavigationComponent,
    ],
})
export class PublicRecipeDetailComponent {
    protected readonly shopping = inject(ShoppingListAddFacade);
    protected readonly addedIngredients = computed(
        () =>
            new Set(
                this.recipe()
                    ?.steps.flatMap(step => step.ingredients)
                    .map((_, index) => index)
                    .filter(index => this.shopping.isAdded(index)),
            ),
    );
    protected readonly recipeCategoryKey = recipeCategoryKey;
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly facade = inject(PublicRecipesFacade);
    private readonly auth = inject(AuthService);
    private readonly authDialog = inject(PublicAuthDialogService);
    private readonly browser = inject(BrowserWindowService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly routeData = toSignal(this.route.data, { initialValue: this.route.snapshot.data });
    protected readonly page = computed(() => readPublicRecipePage(this.routeData()['seo']));
    protected readonly recipe = computed(() => this.page().recipe);
    protected readonly images = computed(() => {
        const recipe = this.recipe();
        return recipe === null ? [] : recipeImages(recipe);
    });
    protected readonly saved = signal(false);
    protected readonly busy = signal(false);
    protected readonly servings = signal(1);
    protected readonly actionMessage = signal<string | null>(null);
    protected readonly shareUrl = signal<string | null>(null);
    protected readonly totalTime = computed(() => {
        const recipe = this.recipe();
        if (recipe === null || (recipe.prepTime === null && recipe.cookTime === null)) {
            return null;
        }
        return (recipe.prepTime ?? 0) + (recipe.cookTime ?? 0);
    });
    protected readonly recipeLanguageKey = computed(() => (this.recipe()?.language === 'ru' ? 'RECIPE_LANGUAGE.RU' : 'RECIPE_LANGUAGE.EN'));
    protected readonly saveLabelKey = computed(() => (this.saved() ? 'PUBLIC_RECIPES.SAVED' : 'PUBLIC_RECIPES.SAVE'));
    protected readonly unavailableTitleKey = computed(() =>
        this.page().error === 'error' ? 'PUBLIC_RECIPES.ERROR' : 'PUBLIC_RECIPES.NOT_FOUND',
    );
    protected readonly showTimeSeparator = computed(() => {
        const recipe = this.recipe();
        return recipe?.prepTime !== null && recipe?.cookTime !== null;
    });
    protected readonly actionMessageKey = computed(() => this.actionMessage() ?? '');
    private readonly language = toSignal(this.translate.onLangChange.pipe(map(event => event.lang)), {
        initialValue: this.translate.getCurrentLang() ?? 'en',
    });
    protected readonly servingsKey = computed(
        () => `PUBLIC_RECIPES.SERVINGS_${new Intl.PluralRules(this.language()).select(this.servings()).toUpperCase()}`,
    );
    public constructor() {
        effect(() => {
            this.servings.set(this.recipe()?.servings ?? 1);
            this.shopping.setScope(this.recipe()?.id ?? '', this.recipe()?.name ?? '');
        });
        effect(onCleanup => {
            const recipe = this.recipe();
            this.saved.set(false);
            this.actionMessage.set(null);
            if (recipe !== null && this.auth.isAuthenticated()) {
                const subscription = this.facade
                    .isFavorite(recipe.id)
                    .pipe(takeUntilDestroyed(this.destroyRef))
                    .subscribe(saved => {
                        this.saved.update(current => current || saved);
                    });
                onCleanup(() => {
                    subscription.unsubscribe();
                });
            }
        });
    }

    protected async backToCatalogAsync(): Promise<void> {
        await this.router.navigate(['/explore'], { queryParamsHandling: 'preserve' });
    }

    protected async actAsync(action: 'save' | 'diary'): Promise<void> {
        if (this.busy()) {
            return;
        }
        if (!this.auth.isAuthenticated()) {
            const dialog = await this.authDialog.openAsync({ mode: 'login', returnUrl: this.router.url, destroyRef: this.destroyRef });
            if (dialog !== null) {
                await firstValueFrom(dialog.afterClosed());
            }
            if (this.destroyRef.destroyed || !this.auth.isAuthenticated()) {
                return;
            }
        }
        const recipe = this.recipe();
        if (recipe === null) {
            return;
        }
        this.busy.set(true);
        this.actionMessage.set(null);
        try {
            if (action === 'save') {
                await this.facade.saveAsync(recipe);
                this.saved.set(true);
            } else {
                await this.facade.addToDiaryAsync(recipe.id);
            }
            this.actionMessage.set(action === 'save' ? 'PUBLIC_RECIPES.SAVED' : 'PUBLIC_RECIPES.ADDED');
        } catch {
            this.actionMessage.set('PUBLIC_RECIPES.ACTION_ERROR');
        } finally {
            this.busy.set(false);
        }
    }

    protected async shareAsync(): Promise<void> {
        const origin = this.browser.getOrigin();
        if (origin === undefined) {
            return;
        }
        const url = `${origin}/explore/${this.recipe()?.id ?? ''}`;
        try {
            await this.browser.copyTextAsync(url);
            this.actionMessage.set('PUBLIC_RECIPES.COPIED');
        } catch {
            this.shareUrl.set(url);
        }
    }

    protected retry(): void {
        void this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' });
    }
}

function readPublicRecipePage(value: unknown): PublicRecipePageData {
    if (isPublicRecipePage(value)) {
        return value;
    }
    return { recipe: null, error: 'error' };
}

function isPublicRecipePage(value: unknown): value is PublicRecipePageData {
    return typeof value === 'object' && value !== null && 'recipe' in value && 'error' in value;
}
