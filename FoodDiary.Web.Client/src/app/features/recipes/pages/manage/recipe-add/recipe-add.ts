import { ChangeDetectionStrategy, Component, viewChild } from '@angular/core';

import type { UnsavedEditor } from '../../../../../shared/forms/unsaved-editor';
import { RecipeManageComponent } from '../../../components/manage/recipe-manage/recipe-manage';

@Component({
    selector: 'app-recipe-add',
    templateUrl: './recipe-add.html',
    styleUrls: ['./recipe-add.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RecipeManageComponent],
})
export class RecipeAddComponent implements UnsavedEditor {
    private readonly editor = viewChild(RecipeManageComponent);

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        return this.editor()?.canLeaveAsync() ?? true;
    }
}
