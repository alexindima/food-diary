import { computed, inject, type Signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import type { FdUiSelectOption } from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../shared/i18n/inject-current-language';
import { RECIPE_CATEGORIES, recipeCategoryKey } from '../models/recipe-category';

export function injectRecipeCategoryOptions(includeAll = false): Signal<Array<FdUiSelectOption<string>>> {
    const translate = inject(TranslateService);
    const language = injectCurrentLanguage();
    return computed(() => {
        language();
        const options = RECIPE_CATEGORIES.map(value => ({ value, label: String(translate.instant(recipeCategoryKey(value))) }));
        return includeAll ? [{ value: '', label: String(translate.instant('PUBLIC_RECIPES.ANY_CATEGORY')) }, ...options] : options;
    });
}
