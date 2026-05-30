import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { RecipeManageComponent } from '../../../components/manage/recipe-manage/recipe-manage';
import type { Recipe } from '../../../models/recipe.data';

@Component({
    selector: 'fd-recipe-edit',
    templateUrl: './recipe-edit.html',
    styleUrls: ['./recipe-edit.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RecipeManageComponent],
})
export class RecipeEditComponent {
    public readonly recipe = input<Recipe | null>(null);
}
