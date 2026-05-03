import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { RecipeManageComponent } from '../../components/manage/recipe-manage.component';
import { type Recipe } from '../../models/recipe.data';

@Component({
    selector: 'fd-recipe-edit',
    templateUrl: './recipe-edit.component.html',
    styleUrls: ['./recipe-edit.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RecipeManageComponent],
})
export class RecipeEditComponent {
    public readonly recipe = input<Recipe | null>(null);
}
