import { computed, inject, type Signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import type { FdUiSelectOption } from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../shared/i18n/inject-current-language';
import { RECIPE_CATEGORIES, type RecipeCategory, recipeCategoryKey } from '../../../shared/models/recipe-category';

/* eslint-disable no-redeclare -- TypeScript overloads separate the catalogue filter sentinel from editor category values. */
export function injectRecipeCategoryOptions(includeAll?: false): Signal<Array<FdUiSelectOption<RecipeCategory>>>;
export function injectRecipeCategoryOptions(includeAll: true): Signal<Array<FdUiSelectOption<RecipeCategory | ''>>>;
export function injectRecipeCategoryOptions(includeAll = false): Signal<Array<FdUiSelectOption<RecipeCategory | ''>>> {
    const translate = inject(TranslateService);
    const language = injectCurrentLanguage();
    return computed(() => {
        language();
        const options = RECIPE_CATEGORIES.map(value => ({ value, label: String(translate.instant(recipeCategoryKey(value))) }));
        return includeAll ? [{ value: '', label: String(translate.instant('PUBLIC_RECIPES.ANY_CATEGORY')) }, ...options] : options;
    });
}
