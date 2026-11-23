import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom } from 'rxjs';
import { expect, it, type Mock, vi } from 'vitest';

import { environment } from '../../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { FavoriteRecipeHttpResponse } from '../../../../../shared/api/sdk/generated/model/favorite-recipe-http-response';
import type { OwnedImageContentHttpResponse } from '../../../../../shared/api/sdk/generated/model/owned-image-content-http-response';
import type { RecipeHttpResponse } from '../../../../../shared/api/sdk/generated/model/recipe-http-response';
import { type Recipe, RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../../../../shared/models/recipe-ingredient';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { ShoppingListAddFacade } from '../../../../shopping-lists/contracts/shopping-list-add';
import type { RecipeDetailActionResult } from '../recipe-detail-lib/recipe-detail.types';
import { RecipeDetailComponent } from './recipe-detail';

const FAVORITES_API = environment.apiUrls.favoriteRecipes;
const RECIPE_ID = entityId<'recipe'>('f3c53983-c66b-4667-908a-75611a213f42');
const FAVORITE_ID = 'fc6c9953-8b27-4d78-981c-b845baadfac6';
const SERVINGS = 2;

function recipe(overrides: Partial<Recipe> = {}): Recipe {
    return {
        id: RECIPE_ID,
        name: 'Stew with rice',
        servings: SERVINGS,
        createdAt: utcInstant('2026-05-01T12:00:00Z'),
        visibility: RecipeVisibility.Private,
        usageCount: 1,
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [
            {
                id: 'first-step',
                stepNumber: 1,
                instruction: 'Serve the stew with cooked rice.',
                ingredients: [
                    recipeIngredientFromStored({
                        id: 'cooked-rice-ingredient',
                        nestedRecipeId: 'cooked-rice',
                        nestedRecipeName: 'Cooked rice',
                        amount: SERVINGS,
                    }),
                ],
            },
        ],
        ...overrides,
    };
}

async function renderDetailsAsync(data: Recipe): Promise<{
    fixture: ComponentFixture<RecipeDetailComponent>;
    host: HTMLElement;
    close: Mock<FdUiDialogRef<unknown, RecipeDetailActionResult>['close']>;
    addAll: Mock<ShoppingListAddFacade['addAllAsync']>;
    http: HttpTestingController;
}> {
    const close = vi.fn<FdUiDialogRef<unknown, RecipeDetailActionResult>['close']>();
    const addAll = vi.fn<ShoppingListAddFacade['addAllAsync']>().mockResolvedValue(undefined);
    TestBed.configureTestingModule({
        imports: [RecipeDetailComponent],
        providers: [
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: data },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    }).overrideComponent(RecipeDetailComponent, {
        add: {
            providers: [
                {
                    provide: ShoppingListAddFacade,
                    useValue: {
                        addAllAsync: addAll,
                        setScope: vi.fn(),
                        isAdded: (): boolean => false,
                        target: signal(null),
                        busy: signal(false),
                        message: signal(null),
                    },
                },
            ],
        },
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { RECIPE_DETAIL: { SUMMARY: { SERVINGS_FEW: 'portions' } } });
    await firstValueFrom(translate.use('en'));
    const fixture = TestBed.createComponent(RecipeDetailComponent);
    fixture.detectChanges();
    return { fixture, host: fixture.nativeElement as HTMLElement, close, addAll, http: TestBed.inject(HttpTestingController) };
}

function button(host: HTMLElement, label: string): HTMLButtonElement {
    const control = [...host.querySelectorAll('button')].find(
        candidate => candidate.getAttribute('aria-label') === label || candidate.textContent.includes(label),
    );
    if (control === undefined) {
        throw new Error(`Expected recipe detail action ${label}`);
    }
    return control;
}

it('allows a used recipe to pass its localized nested servings to the shopping list while editing stays unavailable', async () => {
    const { host, close, addAll, http } = await renderDetailsAsync(recipe());
    http.expectOne({ method: 'GET', url: `${FAVORITES_API}/check/${RECIPE_ID}` }).flush(false);

    expect(host.textContent).toContain('RECIPE_DETAIL.WARNING_IN_USE');
    expect(host.textContent).toContain('RECIPE_DETAIL.DUPLICATE_BUTTON');
    expect(host.textContent).not.toContain('RECIPE_DETAIL.EDIT_BUTTON');
    expect(host.textContent).not.toContain('RECIPE_DETAIL.DELETE_BUTTON');
    button(host, 'RECIPE_DETAIL.ADD_TO_SHOPPING_LIST').click();
    expect(addAll).toHaveBeenCalledExactlyOnceWith([{ index: 0, item: { name: 'Cooked rice', note: '2 portions', isChecked: false } }]);
    expect(close).not.toHaveBeenCalled();

    button(host, 'COMMON.CLOSE').click();
    expect(close).toHaveBeenCalledExactlyOnceWith();
    http.verify();
});

it('localizes the summary quantity of a nested recipe and keeps the authored amount text', async () => {
    const stored = recipe();
    const halfServing = 0.5;
    const authoredAmount = '1/2 ч. л.';
    stored.steps[0].ingredients = [
        recipeIngredientFromStored({
            id: 'cooked-rice-ingredient',
            nestedRecipeId: 'cooked-rice',
            nestedRecipeName: 'Cooked rice',
            amount: halfServing,
        }),
        recipeIngredientFromStored({ id: 'salt-ingredient', textName: 'Salt', amountText: authoredAmount, amount: 0 }),
    ];
    const { fixture, host, http } = await renderDetailsAsync(stored);
    http.expectOne({ method: 'GET', url: `${FAVORITES_API}/check/${RECIPE_ID}` }).flush(false);
    const amounts = (): string[] =>
        Array.from(
            host.querySelectorAll('[role="tabpanel"]:not([hidden]) fd-recipe-detail-ingredient-preview .recipe-detail__list-row strong'),
            amount => amount.textContent.replaceAll(/\s+/gu, ' ').trim(),
        );

    expect(amounts()).toEqual(['0.5 portions', authoredAmount]);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('ru', { RECIPE_DETAIL: { SUMMARY: { SERVINGS_FEW: 'порции' } } });
    await firstValueFrom(translate.use('ru'));
    fixture.detectChanges();

    expect(amounts()).toEqual(['0,5 порции', authoredAmount]);
    http.verify();
});

it('favorites a public recipe once before repaint and cancels the obsolete status check', async () => {
    const shared = recipe({ isOwnedByCurrentUser: false, visibility: RecipeVisibility.Public });
    const { fixture, host, close, http } = await renderDetailsAsync(shared);
    const status = http.expectOne({ method: 'GET', url: `${FAVORITES_API}/check/${RECIPE_ID}` });
    expect(host.textContent).toContain('RECIPE_DETAIL.WARNING_NOT_OWNER');
    const favorite = button(host, 'RECIPE_DETAIL.ADD_FAVORITE');
    favorite.click();
    favorite.click();
    const add = http.expectOne({ method: 'POST', url: FAVORITES_API });
    expect(add.request.body).toMatchObject({ recipeId: RECIPE_ID });
    expect(status.cancelled).toBe(true);
    fixture.detectChanges();
    expect(favorite.disabled).toBe(true);

    add.flush({
        id: FAVORITE_ID,
        recipeId: RECIPE_ID,
        recipeName: shared.name,
        createdAtUtc: '2026-05-02T12:00:00Z',
        servings: SERVINGS,
        ingredientCount: 1,
    } satisfies FavoriteRecipeHttpResponse);
    fixture.detectChanges();
    expect(button(host, 'RECIPE_DETAIL.REMOVE_FAVORITE').disabled).toBe(false);
    expect(close).not.toHaveBeenCalled();

    button(host, 'COMMON.CLOSE').click();
    expect(close).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({ id: RECIPE_ID, action: 'FavoriteChanged', favoriteChanged: true }),
    );
    http.verify();
});

it('retries a failed publication refresh and shows the current owner photo after publication is withdrawn', async () => {
    const oldAsset = entityId<'image-asset'>('bdcd39fc-f901-4b09-864f-3f70e581ace9');
    const newAsset = entityId<'image-asset'>('22404104-99f8-4ca4-8da6-674c00ea4053');
    const oldPreview = 'data:image/webp;base64,UklGRg==';
    const newPreview = 'data:image/png;base64,iVBORw0KGgo=';
    const note = 'Please use a photo of the finished dish.';
    const submitted = recipe({ imageAssetId: oldAsset, publicationStatus: 'ChangesRequested', publicationReason: note });
    const { fixture, host, http } = await renderDetailsAsync(submitted);
    const status = (): string | undefined => host.querySelector('fd-recipe-publication-notice [role="status"]')?.textContent;
    const cover = (): string | null | undefined => host.querySelector('fd-public-recipe-gallery .gallery-cover img')?.getAttribute('src');
    const recipeUrl = `${environment.apiUrls.recipes}/${RECIPE_ID}?includePublic=false`;
    const failedMessage = 'RECIPE_PUBLICATION.REFRESH_FAILED';

    http.expectOne({ method: 'GET', url: `${FAVORITES_API}/check/${RECIPE_ID}` }).flush(false);
    expect(cover()).toBeUndefined();
    http.expectOne({ method: 'GET', url: `${environment.apiUrls.images}/${oldAsset}/content` }).flush({
        dataUrl: oldPreview,
    } satisfies OwnedImageContentHttpResponse);
    fixture.detectChanges();
    expect(status()).toContain('RECIPE_PUBLICATION.ChangesRequested');
    expect(status()).toContain(note);
    expect(cover()).toBe(oldPreview);

    const refresh = button(host, 'RECIPE_PUBLICATION.REFRESH');
    refresh.click();
    refresh.click();
    const failedRead = http.expectOne({ method: 'GET', url: recipeUrl });
    fixture.detectChanges();
    expect(refresh.disabled).toBe(true);
    failedRead.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(status()).toContain(failedMessage);
    expect(status()).toContain(note);
    expect(cover()).toBe(oldPreview);

    expect(refresh.disabled).toBe(false);
    refresh.click();
    const retry = http.expectOne({ method: 'GET', url: recipeUrl });
    fixture.detectChanges();
    expect(status()).not.toContain(failedMessage);
    retry.flush({
        id: submitted.id,
        name: submitted.name,
        createdAt: submitted.createdAt,
        servings: submitted.servings,
        usageCount: submitted.usageCount,
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        visibility: RecipeVisibility.Private,
        publicationStatus: null,
        imageAssetId: newAsset,
        steps: [],
    } satisfies RecipeHttpResponse);
    fixture.detectChanges();
    expect(status()).toBeUndefined();
    expect(refresh.isConnected).toBe(false);
    expect(cover()).toBeUndefined();

    http.expectOne({ method: 'GET', url: `${environment.apiUrls.images}/${newAsset}/content` }).flush({
        dataUrl: newPreview,
    } satisfies OwnedImageContentHttpResponse);
    fixture.detectChanges();
    expect(cover()).toBe(newPreview);
    http.verify();
});
