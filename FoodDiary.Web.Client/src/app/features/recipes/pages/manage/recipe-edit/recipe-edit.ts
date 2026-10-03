import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { Recipe } from '../../../../../shared/models/recipe.data';
import { RecipeManageComponent } from '../../../components/manage/recipe-manage/recipe-manage';

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
