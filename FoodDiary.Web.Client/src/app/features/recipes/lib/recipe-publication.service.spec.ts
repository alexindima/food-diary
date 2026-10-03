import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ProductVisibility } from '../../../shared/models/product.data';
import { type RecipeDto, RecipeVisibility } from '../../../shared/models/recipe.data';
import { ProductPublicationService } from '../../products/contracts/product-publication';
import { RecipePublicationService } from './recipe-publication.service';

describe('RecipePublicationService', () => {
    const products = { loadAsync: vi.fn(), publishAsync: vi.fn() };
    const dialogs = { open: vi.fn() };
    const data: RecipeDto = {
        name: 'Recipe',
        servings: 1,
        visibility: RecipeVisibility.Public,
        calculateNutritionAutomatically: true,
        steps: [
            {
                description: 'Mix',
                ingredients: [
                    { productId: 'private', amount: 150.5 },
                    { productId: 'public', amount: 100 },
                    { textName: 'Salt', amountText: 'to taste', amount: 0 },
                ],
            },
        ],
    };
    let service: RecipePublicationService;
    beforeEach(() => {
        vi.resetAllMocks();
        products.loadAsync.mockResolvedValue([
            { id: 'private', name: 'Йогурт', baseUnit: 'G', visibility: ProductVisibility.Private, isOwnedByCurrentUser: true },
            { id: 'public', name: 'Milk', baseUnit: 'ML', visibility: ProductVisibility.Public },
        ]);
        products.publishAsync.mockResolvedValue(undefined);
        TestBed.configureTestingModule({
            providers: [
                { provide: ProductPublicationService, useValue: products },
                { provide: FdUiDialogService, useValue: dialogs },
                { provide: TranslateService, useValue: { instant: (): string => 'г', getCurrentLang: (): string => 'ru' } },
            ],
        });
        service = TestBed.inject(RecipePublicationService);
    });

    it('replaces only private references and retains localized quantities', async () => {
        dialogs.open.mockReturnValue({ afterClosed: () => of('text') });
        const result = await service.prepareAsync(data);
        expect(result?.steps[0]?.ingredients).toEqual([
            { textName: 'Йогурт', amountText: '150,5 г', amount: 0 },
            data.steps[0]?.ingredients[1],
            data.steps[0]?.ingredients[2],
        ]);
        expect(data.steps[0]?.ingredients[0]?.productId).toBe('private');
        expect(products.publishAsync).not.toHaveBeenCalled();
    });

    it('publishes products only after the explicit publish choice', async () => {
        dialogs.open.mockReturnValue({ afterClosed: () => of('publish') });
        expect(await service.prepareAsync(data)).toBe(data);
        expect(products.publishAsync).toHaveBeenCalledWith([expect.objectContaining({ id: 'private' })]);
    });

    it('cancels without changes', async () => {
        dialogs.open.mockReturnValue({ afterClosed: () => of(undefined) });
        expect(await service.prepareAsync(data)).toBeNull();
        expect(products.publishAsync).not.toHaveBeenCalled();
    });

    it('does not inspect private recipes', async () => {
        const privateRecipe = { ...data, visibility: RecipeVisibility.Private };
        expect(await service.prepareAsync(privateRecipe)).toBe(privateRecipe);
        expect(products.loadAsync).not.toHaveBeenCalled();
    });

    it('skips the dialog when current products are public', async () => {
        products.loadAsync.mockResolvedValue([{ id: 'private', visibility: ProductVisibility.Public }]);
        expect(await service.prepareAsync(data)).toBe(data);
        expect(dialogs.open).not.toHaveBeenCalled();
    });

    it('blocks saving when verification or publication fails', async () => {
        products.loadAsync.mockRejectedValueOnce(new Error('unavailable'));
        await expect(service.prepareAsync(data)).rejects.toThrow('unavailable');
        dialogs.open.mockReturnValue({ afterClosed: () => of('publish') });
        products.publishAsync.mockRejectedValueOnce(new Error('forbidden'));
        await expect(service.prepareAsync(data)).rejects.toThrow('forbidden');
    });
});
