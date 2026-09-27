import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { resolveServingsUnitKey } from '../../../lib/recipe-servings.utils';
import type { Recipe } from '../../../models/recipe.data';
import { recipeCategoryKey } from '../../../models/recipe-category';
import type { RecipeSelectItemViewModel } from '../recipe-select-dialog-lib/recipe-select-dialog.types';

@Component({
    selector: 'fd-recipe-select-dialog-content',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent, FdUiLoaderComponent],
    templateUrl: './recipe-select-dialog-content.html',
    styleUrl: '../recipe-select-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeSelectDialogContentComponent {
    protected readonly recipeCategoryKey = recipeCategoryKey;
    protected readonly servingsUnitKey = resolveServingsUnitKey;
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<readonly RecipeSelectItemViewModel[]>();

    public readonly recipeSelected = output<Recipe>();
}
