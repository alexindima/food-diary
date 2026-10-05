import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { Recipe } from '../../../shared/models/recipe.data';

export type RecipeLookup = { getById: (id: string) => Observable<Recipe | null> };
export const RECIPE_LOOKUP = new InjectionToken<RecipeLookup>('RecipeLookup');
