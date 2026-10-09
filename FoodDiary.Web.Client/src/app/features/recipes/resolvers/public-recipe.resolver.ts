import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject, RESPONSE_INIT } from '@angular/core';
import type { ResolveFn } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import type { SeoData } from '../../../services/seo.service';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { PublicRecipeService } from '../api/public-recipe.service';
import { publicRecipeSeo } from '../lib/public-recipe.utils';
import type { PublicRecipe } from '../models/public-recipe.data';

export type PublicRecipePageData = SeoData & { recipe: PublicRecipe | null; error: 'not-found' | 'error' | null };

export const publicRecipeResolver: ResolveFn<PublicRecipePageData> = route => {
    const response = inject(RESPONSE_INIT, { optional: true });
    if (response !== null) {
        const headers = new Headers(response.headers);
        headers.set('Cache-Control', 'no-store');
        response.headers = headers;
    }
    return inject(PublicRecipeService)
        .getById(entityId<'recipe'>(route.paramMap.get('id') ?? ''))
        .pipe(
            map(recipe => ({ ...publicRecipeSeo(recipe), recipe, error: null })),
            catchError((error: unknown) => {
                const notFound =
                    error instanceof HttpErrorResponse &&
                    (error.status === Number(HttpStatusCode.NotFound) || error.status === Number(HttpStatusCode.BadRequest));
                if (response !== null) {
                    response.status = notFound ? HttpStatusCode.NotFound : HttpStatusCode.ServiceUnavailable;
                }
                return of<PublicRecipePageData>({
                    recipe: null,
                    error: notFound ? 'not-found' : 'error',
                    noIndex: true,
                    titleKey: notFound ? 'PUBLIC_RECIPES.NOT_FOUND' : 'PUBLIC_RECIPES.ERROR',
                });
            }),
        );
};
