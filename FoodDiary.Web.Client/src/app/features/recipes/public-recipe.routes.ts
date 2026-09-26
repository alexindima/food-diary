import type { Type } from '@angular/core';
import type { Routes } from '@angular/router';

import { publicRecipeResolver } from './resolvers/public-recipe.resolver';

export default [
    {
        path: '',
        loadComponent: async (): Promise<Type<unknown>> =>
            import('./pages/public-catalog/public-catalog').then(m => m.PublicRecipeCatalogComponent),
    },
    {
        path: ':id',
        resolve: { seo: publicRecipeResolver },
        runGuardsAndResolvers: 'always',
        loadComponent: async (): Promise<Type<unknown>> =>
            import('./pages/public-detail/public-detail').then(m => m.PublicRecipeDetailComponent),
    },
] satisfies Routes;
