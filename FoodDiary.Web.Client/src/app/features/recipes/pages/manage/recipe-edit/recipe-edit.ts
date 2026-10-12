import { ChangeDetectionStrategy, Component, input, viewChild } from '@angular/core';

import type { UnsavedEditor } from '../../../../../shared/forms/unsaved-editor';
import type { Recipe } from '../../../../../shared/models/recipe.data';
import { RecipeManageComponent } from '../../../components/manage/recipe-manage/recipe-manage';

@Component({
    selector: 'fd-recipe-edit',
    templateUrl: './recipe-edit.html',
    styleUrls: ['./recipe-edit.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RecipeManageComponent],
})
export class RecipeEditComponent implements UnsavedEditor {
    private readonly editor = viewChild(RecipeManageComponent);
    public readonly recipe = input<Recipe | null>(null);

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        return this.editor()?.canLeaveAsync() ?? true;
    }
}
