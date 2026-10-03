import { inject, Service } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { ProductVisibility } from '../../../shared/models/product.data';
import { type RecipeDto, RecipeVisibility } from '../../../shared/models/recipe.data';
import { ProductPublicationService } from '../../products/contracts/product-publication';
import {
    type RecipePublicationChoice,
    RecipePublicationDialogComponent,
    type RecipePublicationDialogData,
} from '../dialogs/recipe-publication-dialog/recipe-publication-dialog';

@Service()
export class RecipePublicationService {
    private readonly products = inject(ProductPublicationService);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);

    public async prepareAsync(data: RecipeDto): Promise<RecipeDto | null> {
        if (data.visibility !== RecipeVisibility.Public) {
            return data;
        }
        const ids = data.steps.flatMap(step =>
            step.ingredients.flatMap(ingredient => (ingredient.productId !== undefined ? [ingredient.productId] : [])),
        );
        const products = await this.products.loadAsync(ids);
        const privateProducts = products.filter(product => product.visibility !== ProductVisibility.Public);
        if (privateProducts.length === 0) {
            return data;
        }
        const choice = await firstValueFrom(
            this.dialogs
                .open<RecipePublicationDialogComponent, RecipePublicationDialogData, RecipePublicationChoice>(
                    RecipePublicationDialogComponent,
                    {
                        preset: 'form',
                        data: {
                            names: privateProducts.map(product => product.name),
                            canPublish: privateProducts.every(product => product.isOwnedByCurrentUser),
                        },
                    },
                )
                .afterClosed(),
        );
        if (choice === 'publish') {
            await this.products.publishAsync(privateProducts);
            return data;
        }
        if (choice !== 'text') {
            return null;
        }
        const byId = new Map(privateProducts.map(product => [product.id, product]));
        return {
            ...data,
            steps: data.steps.map(step => ({
                ...step,
                ingredients: step.ingredients.map(ingredient => {
                    const product = byId.get(ingredient.productId ?? '');
                    if (product === undefined) {
                        return ingredient;
                    }
                    const unit = this.translate.instant(`PRODUCT_AMOUNT_UNITS_SHORT.${product.baseUnit}`);
                    const amount = new Intl.NumberFormat(this.translate.getCurrentLang() ?? 'en', { maximumFractionDigits: 20 }).format(
                        ingredient.amount,
                    );
                    return { textName: product.name, amountText: `${amount} ${unit}`, amount: 0 };
                }),
            })),
        };
    }
}
