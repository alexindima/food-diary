import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import type { Recipe } from '../../models/recipe.data';
import type { RecipeSelectItemViewModel } from './recipe-select-dialog.types';

@Component({
    selector: 'fd-recipe-select-dialog-content',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent, FdUiLoaderComponent],
    templateUrl: './recipe-select-dialog-content.component.html',
    styleUrl: './recipe-select-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeSelectDialogContentComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<readonly RecipeSelectItemViewModel[]>();

    public readonly recipeSelected = output<Recipe>();
}
