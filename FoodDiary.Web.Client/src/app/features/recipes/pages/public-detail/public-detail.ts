import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogService, FdUiIconComponent } from 'fd-ui-kit';
import { firstValueFrom, map } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { AuthService } from '../../../../services/auth.service';
import { recipeCategoryKey } from '../../../../shared/models/recipe-category';
import { entityId } from '../../../../shared/models/semantics/entity-id';
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
import type { PublicRecipe } from '../../models/public-recipe.data';
import type { PublicRecipePageData } from '../../resolvers/public-recipe.resolver';

type RecipeAction = { recipe: PublicRecipe; kind: 'save' | 'diary' };

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
    private readonly dialogs = inject(FdUiDialogService);
    private readonly browser = inject(BrowserWindowService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private qrDialogActive = false;
    private closeQrDialog?: () => void;
    private readonly activeAction = signal<RecipeAction | null>(null);
    protected readonly seo = input.required<PublicRecipePageData>();
    protected readonly recipe = computed(() => this.seo().recipe);
    protected readonly recipeDetail = computed(() => {
        const recipe = this.recipe();
        return recipe === null ? null : { recipe, images: recipeImages(recipe) };
    });
    protected readonly saved = signal(false);
    protected readonly busy = computed(() => this.activeAction()?.recipe === this.recipe());
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
        this.seo().error === 'error' ? 'PUBLIC_RECIPES.ERROR' : 'PUBLIC_RECIPES.NOT_FOUND',
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
    private readonly favoriteStatus = rxResource({
        params: () => {
            const recipe = this.recipe();
            return recipe !== null && this.auth.isAuthenticated() ? { id: entityId<'recipe'>(recipe.id) } : undefined;
        },
        stream: ({ params }) => this.facade.isFavorite(params.id),
    });

    public constructor() {
        effect(onCleanup => {
            this.servings.set(this.recipe()?.servings ?? 1);
            this.shareUrl.set(null);
            this.shopping.setScope(this.recipe()?.id ?? '', this.recipe()?.name ?? '');
            onCleanup(() => {
                this.closeQrDialog?.();
            });
        });
        effect(() => {
            this.recipe();
            this.auth.isAuthenticated();
            this.saved.set(false);
            this.actionMessage.set(null);
        });
        effect(() => {
            if (this.favoriteStatus.hasValue()) {
                const saved = this.favoriteStatus.value();
                this.saved.update(current => current || saved);
            } else if (this.favoriteStatus.error() !== undefined) {
                this.actionMessage.set('PUBLIC_RECIPES.ACTION_ERROR');
            }
        });
    }

    protected async backToCatalogAsync(): Promise<void> {
        await this.router.navigate(['/explore'], { queryParamsHandling: 'preserve' });
    }

    protected async actAsync(kind: RecipeAction['kind']): Promise<void> {
        const recipe = this.recipe();
        if (recipe === null || this.busy()) {
            return;
        }
        const action = { recipe, kind };
        this.activeAction.set(action);
        try {
            if (!this.auth.isAuthenticated() && !(await this.authenticateForActionAsync(action))) {
                return;
            }
            this.actionMessage.set(null);
            if (kind === 'save') {
                await this.facade.saveAsync(recipe);
            } else {
                await this.facade.addToDiaryAsync(entityId<'recipe'>(recipe.id), this.servings());
            }
            this.publishActionResult(action);
        } catch {
            if (this.isCurrentAction(action)) {
                this.actionMessage.set('PUBLIC_RECIPES.ACTION_ERROR');
            }
        } finally {
            if (this.activeAction() === action) {
                this.activeAction.set(null);
            }
        }
    }

    private async authenticateForActionAsync(action: RecipeAction): Promise<boolean> {
        const dialog = await this.authDialog.openAsync({ mode: 'login', returnUrl: this.router.url, destroyRef: this.destroyRef });
        if (dialog !== null) {
            await firstValueFrom(dialog.afterClosed(), { defaultValue: undefined });
        }
        return this.isCurrentAction(action) && this.auth.isAuthenticated();
    }

    private publishActionResult(action: RecipeAction): void {
        if (!this.isCurrentAction(action)) {
            return;
        }
        if (action.kind === 'save') {
            this.saved.set(true);
        }
        this.actionMessage.set(action.kind === 'save' ? 'PUBLIC_RECIPES.SAVED' : 'PUBLIC_RECIPES.ADDED');
    }

    private isCurrentAction(action: RecipeAction): boolean {
        return this.activeAction() === action && this.isCurrentRecipe(action.recipe);
    }

    private isCurrentRecipe(recipe: PublicRecipe): boolean {
        // The snapshot advances before the bound input updates on a reused route.
        return !this.destroyRef.destroyed && this.recipe() === recipe && this.route.snapshot.data['seo'] === this.seo();
    }

    protected async shareAsync(): Promise<void> {
        const recipe = this.recipe();
        if (recipe === null) {
            return;
        }
        const url = this.publicRecipeUrl(recipe);
        if (url === undefined) {
            return;
        }
        try {
            await this.browser.copyTextAsync(url);
            if (this.isCurrentRecipe(recipe)) {
                this.actionMessage.set('PUBLIC_RECIPES.COPIED');
            }
        } catch {
            if (this.isCurrentRecipe(recipe)) {
                this.shareUrl.set(url);
            }
        }
    }

    protected async showQrCodeAsync(): Promise<void> {
        const recipe = this.recipe();
        if (recipe === null || this.qrDialogActive) {
            return;
        }
        const url = this.publicRecipeUrl(recipe);
        if (url === undefined) {
            return;
        }

        this.qrDialogActive = true;
        try {
            const { PublicRecipeQrDialogComponent } = await import('../../dialogs/public-recipe-qr-dialog/public-recipe-qr-dialog');
            if (!this.isCurrentRecipe(recipe)) {
                return;
            }
            const dialog = this.dialogs.open(PublicRecipeQrDialogComponent, { size: 'sm', data: { url } });
            this.closeQrDialog = (): void => {
                dialog.close();
            };
            await firstValueFrom(dialog.afterClosed(), { defaultValue: undefined });
        } catch {
            if (this.isCurrentRecipe(recipe)) {
                this.actionMessage.set('PUBLIC_RECIPES.ACTION_ERROR');
            }
        } finally {
            this.closeQrDialog = undefined;
            this.qrDialogActive = false;
        }
    }

    private publicRecipeUrl(recipe: PublicRecipe): string | undefined {
        const origin = this.browser.getOrigin();
        return origin === undefined ? undefined : `${origin}/explore/${recipe.id}`;
    }

    protected retry(): void {
        void this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' });
    }
}
