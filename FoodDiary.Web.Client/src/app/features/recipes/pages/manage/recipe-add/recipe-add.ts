import { ChangeDetectionStrategy, Component } from '@angular/core';

import { RecipeManageComponent } from '../../../components/manage/recipe-manage/recipe-manage';

@Component({
    selector: 'app-recipe-add',
    templateUrl: './recipe-add.html',
    styleUrls: ['./recipe-add.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RecipeManageComponent],
})
export class RecipeAddComponent {}
